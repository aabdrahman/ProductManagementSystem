using Microsoft.EntityFrameworkCore;
using ProductManagementSystem.Api.Data;
using ProductManagementSystem.Api.Entities.Models;
using ProductManagementSystem.Api.Helpers;
using ProductManagementSystem.Api.Services.Contracts;
using ProductManagementSystem.Api.Utilities.Contracts;
using ProductManagementSystem.Shared.DataTransferObjects.Product;
using ProductManagementSystem.Shared.DataTransferObjects.Response;
using Serilog;
using System.Data.Common;
using System.Text.Json;

namespace ProductManagementSystem.Api.Services;

public sealed class ProductService : IProductService
{
    private string _methodName = "MethodName";
    private string _className = "ClassName";

    private readonly RepositoryContext _repositoryContext;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IRedisService _redisService;
    public ProductService(RepositoryContext repositoryContext, IHttpContextAccessor httpContextAccessor, IRedisService redisService)
    {
        _repositoryContext = repositoryContext;
        _httpContextAccessor = httpContextAccessor;
        _redisService = redisService;
    }
    public async Task<GenericResponse<ProductDto>> CreateAsync(CreateProductDto productToCreate)
    {
        try
        {
            Log.ForContext(_className, "ProductService").ForContext(_methodName, "CreateAsync").Information("Creating New Product - {productToCreate}", JsonSerializer.Serialize(productToCreate));

            bool isSimilarExists = await _repositoryContext.Products.IgnoreQueryFilters().AnyAsync(x => x.ProductCategoryId == productToCreate.CategoryId && x.NormalizedName == productToCreate.Name.ToUpper());

            if (isSimilarExists)
            {
                Log.ForContext(_className, "ProductService").ForContext(_methodName, "CreateAsync").Information("Product with Name: {Name} already exists under category: {CategoryId}.", productToCreate.Name, productToCreate.CategoryId);
                return GenericResponse<ProductDto>.Failure(null, "Product with name already exists under category.", System.Net.HttpStatusCode.Conflict);
            }

            bool categoryExists = await _repositoryContext.ProductCategories.AnyAsync(x => x.Id == productToCreate.CategoryId);

            if (!categoryExists)
            {
                Log.ForContext(_className, "ProductService").ForContext(_methodName, "CreateAsync").Information("Product Creation Failed. Category with Id does not exist - {CategoryId}", productToCreate.CategoryId);
                return GenericResponse<ProductDto>.Failure(null, "No category with specified Category", System.Net.HttpStatusCode.NotFound);
            }

            Product productToInsert = new Product() 
            {
                Name = productToCreate.Name,
                CostPrice = productToCreate.CostPrice,
                CurrentCount = productToCreate.CurrentCount,
                SellingPrice = productToCreate.SellingPrice,
                Description = productToCreate.Description ?? "",
                ProductCategoryId = productToCreate.CategoryId
            };

            await _repositoryContext.Products.AddAsync(productToInsert);

            await _repositoryContext.SaveChangesAsync();

            ProductDto createdProduct = new ProductDto()
            {
                Id = productToInsert.Id,
                Name = productToInsert.NormalizedName,
                CategoryName = productToInsert?.productCategory?.NormalizedName ?? "",
                CostPrice = productToInsert.CostPrice,
                SellingPrice = productToInsert.SellingPrice,
                Description = productToInsert.Description,
                CurrentCount = productToInsert.CurrentCount
            };

            var removeFromCache = await _redisService.RemoveItemAsync(RedisCacheHelperClass.ProductsKey);

            Log.ForContext(_className, "ProductService").ForContext(_methodName, "CreateAsync").Information("Product Creation Successful - {createdProduct}. Remove Cached Products returns - {1}", JsonSerializer.Serialize(createdProduct), removeFromCache);

            return GenericResponse<ProductDto>.Success(createdProduct, "Product Created Successfully.", System.Net.HttpStatusCode.OK);
        }
        catch(DbUpdateException ex)
        {
            Log.ForContext(_className, "ProductService").ForContext(_methodName, "CreateAsync").Error(ex, "Error Inserting product to Database.");
            return GenericResponse<ProductDto>.Failure(null, "Error Inserting product to Database.", System.Net.HttpStatusCode.InternalServerError, new { Message = ex.Message });
        }
        catch (Exception ex)
        {
            Log.ForContext(_className, "ProductService").ForContext(_methodName, "CreateAsync").Error(ex, "Error Occurred Creating product.");
            return GenericResponse<ProductDto>.Failure(null, "Error Occurred Creating product", System.Net.HttpStatusCode.InternalServerError, new { Message = ex.Message });
        }
    }

    public async Task<GenericResponse<string>> DeleteAsync(int Id, bool isSoftDelete = true)
    {
        try
        {
            Log.ForContext(_className, "ProductService").ForContext(_methodName, "DeleteAsync").Information("Removing Product with Id - {Id}. Parameter - {isSoftDelete}", Id, isSoftDelete);

            Product? productToRemove = await _repositoryContext.Products.SingleOrDefaultAsync(x  => x.Id == Id);

            if(productToRemove is null)
            {
                Log.ForContext(_className, "ProductService").ForContext(_methodName, "DeleteAsync").Information("Product with Id does not exist - {Id}", Id);
                return GenericResponse<string>.Failure("Operation Failed.", $"Product with Id: {Id} does not exist", System.Net.HttpStatusCode.NotFound);
            }

            if (isSoftDelete)
            {
                productToRemove.IsActive = false;
                //_repositoryContext.Products.Update(productToRemove);
            }
            else
            {
                //bool isOrderExists = await _repositoryContext.Orders.AnyAsync(x => x.ProductId == Id && (x.OrderStatus == Entities.StaticValues.OrderStatus.Pending || x.OrderStatus == Entities.StaticValues.OrderStatus.Processing));

                bool isOrderExists = await _repositoryContext.OrderLineItems.AnyAsync(x => x.ProductId == Id && x.order.OrderStatus != Entities.StaticValues.OrderStatus.Delivered && x.order.IsActive);

                if (isOrderExists)
                {
                    Log.ForContext(_className, "ProductService").ForContext(_methodName, "DeleteAsync").Information("The product to remove has one or more orders still not concluded");
                    return GenericResponse<string>.Failure("Operation Failed", "Pending order exists for selected product to remove.", System.Net.HttpStatusCode.Conflict);
                }

                _repositoryContext.Products.Remove(productToRemove);
            }

            await _repositoryContext.SaveChangesAsync();

            var removeFromCache = await _redisService.RemoveMultiple(RedisCacheHelperClass.ProductsKey, RedisCacheHelperClass.GetProductCacheKey(Id));

            Log.ForContext(_className, "ProductService").ForContext(_methodName, "DeleteAsync").Information((isSoftDelete ? $"Product successfully marked as inactive - {Id}. Remove Item from cache - {removeFromCache}" : $"Product with Id sucessfully removed - {Id}. Remove Item from cache - {removeFromCache}"));

            return GenericResponse<string>.Success("", $"{(isSoftDelete ? $"Product successfully deactivated" : $"Product successfully removed.")}", System.Net.HttpStatusCode.OK);

        }
        catch(DbUpdateException ex)
        {
            Log.ForContext(_className, "ProductService").ForContext(_methodName, "DeleteAsync").Error(ex, "Database Error Occurred performing product delete operation.");
            return GenericResponse<string>.Failure("Operation Failed", "Database Error Occurred performing product delete operation.", System.Net.HttpStatusCode.InternalServerError, new { Message = ex.Message });    
        }
        catch (Exception ex)
        {
            Log.ForContext(_className, "ProductService").ForContext(_methodName, "DeleteAsync").Error(ex, "Error Occurred performing product delete operation.");
            return GenericResponse<string>.Failure("Operation Failed", "Error Occurred performing product delete operation.", System.Net.HttpStatusCode.InternalServerError, new { Message = ex.Message });
        }
    }

    public async Task<GenericResponse<IEnumerable<ProductDto>>> GetAllAsync()
    {
        try
        {
            string ipAddress = _httpContextAccessor.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "";

            Log.ForContext(_className, "ProductService").ForContext(_methodName, "GetAllAsync").Information("Fetching All Products From {0}.....", ipAddress);

            var productsFromCcahe = await _redisService.GetItemAsync<List<ProductDto>>(RedisCacheHelperClass.ProductsKey);

            if(productsFromCcahe is not null && productsFromCcahe.Any())
            {
                Log.ForContext(_className, "ProductService").ForContext(_methodName, "GetAllAsync").Information("Products Fetched from cache - {0}", productsFromCcahe);
                return GenericResponse<IEnumerable<ProductDto>>.Success(productsFromCcahe, "Products Fetched Successfully.", System.Net.HttpStatusCode.OK);
            }


            List<ProductDto> products = await _repositoryContext.Products
                                        .AsNoTracking()
                                        .Include(X => X.productCategory)
                                        .Select(x =>
                                                    new ProductDto()
                                                    {
                                                        Id = x.Id,
                                                        Name = x.NormalizedName,
                                                        CategoryName = x.productCategory.NormalizedName,
                                                        CostPrice = x.CostPrice,
                                                        SellingPrice = x.SellingPrice,
                                                        Description = x.Description,
                                                        CurrentCount = x.CurrentCount
                                                    })
                                        .ToListAsync();
            if (products.Any())
            {
                var setItemToCache = await _redisService.SetItemAsync(products, RedisCacheHelperClass.ProductsKey);

            }

            Log.ForContext(_className, "ProductService").ForContext(_methodName, "GetAllAsync").Information("Products Fetched Successfully - {products}", JsonSerializer.Serialize(products));

            return GenericResponse<IEnumerable<ProductDto>>.Success(products, "Products Fetched Successfully.", System.Net.HttpStatusCode.OK);    


        }
        catch(DbUpdateException ex)
        {
            Log.ForContext(_className, "ProductService").ForContext(_methodName, "GetAllAsync").Error(ex, "Error Retrieving Products from Database.");
            return GenericResponse<IEnumerable<ProductDto>>.Failure(null, "Error Retrieving Products from Database", System.Net.HttpStatusCode.InternalServerError, new { Message = ex.Message });
        }
        catch (Exception ex)
        {
            Log.ForContext(_className, "ProductService").ForContext(_methodName, "GetAllAsync").Error(ex, "Error Occurred Retrieving Products.");
            return GenericResponse<IEnumerable<ProductDto>>.Failure(null, "Error Occurred Retrieving Products", System.Net.HttpStatusCode.InternalServerError, new { Message = ex.Message });
        }
    }

    public async Task<GenericResponse<IEnumerable<ProductDto>>> GetByCategoryIdAsync(int CategoryId)
    {
        try
        {
            Log.ForContext(_className, "ProductService").ForContext(_methodName, "GetByCategoryIdAsync").Information("Fetching Products By Category - {CategoryId}", CategoryId);

            List<ProductDto> products = await _repositoryContext.Products.Include(x => x.productCategory)
                                                .AsNoTracking()
                                                .Where(x => x.ProductCategoryId == CategoryId)
                                                .Select(x =>
                                                    new ProductDto()
                                                    {
                                                        Id = x.Id,
                                                        Name = x.NormalizedName,
                                                        CategoryName = x.productCategory.NormalizedName,
                                                        CostPrice = x.CostPrice,
                                                        SellingPrice = x.SellingPrice,
                                                        Description = x.Description,
                                                        CurrentCount = x.CurrentCount
                                                    })
                                                .ToListAsync();

            if(!products.Any())
            {
                Log.ForContext(_className, "ProductService").ForContext(_methodName, "GetByCategoryIdAsync").Information("No Products under Category - {CategoryId}", CategoryId);
                return GenericResponse<IEnumerable<ProductDto>>.Failure(null, "No Products under selected category", System.Net.HttpStatusCode.NotFound);
            }

            Log.ForContext(_className, "ProductService").ForContext(_methodName, "GetByCategoryIdAsync").Information("Products Under Category Fetched Successfully - {product}", JsonSerializer.Serialize(products));

            return GenericResponse<IEnumerable<ProductDto>>.Success(products, "Products Under Category Fetched Successfully", System.Net.HttpStatusCode.OK);

        }
        catch (DbUpdateException ex)
        {
            Log.ForContext(_className, "ProductService").ForContext(_methodName, "GetByCategoryIdAsync").Error(ex, "Error Retrieving Products from Database.");
            return GenericResponse<IEnumerable<ProductDto>>.Failure(null, "Error Retrieving Products from Database", System.Net.HttpStatusCode.InternalServerError, new { Message = ex.Message });
        }
        catch (Exception ex)
        {
            Log.ForContext(_className, "ProductService").ForContext(_methodName, "GetByCategoryIdAsync").Error(ex, "Error Occurred Retrieving Products.");
            return GenericResponse<IEnumerable<ProductDto>>.Failure(null, "Error Occurred Retrieving Products", System.Net.HttpStatusCode.InternalServerError, new { Message = ex.Message });
        }
    }

    public async Task<GenericResponse<ProductDto>> GetByIdAsync(int Id)
    {
        try
        {
            Log.ForContext(_className, "ProductService").ForContext(_methodName, "GetByIdAsync").Information("Fetching Product with Id - {Id}", Id);

            var productFromCache = await _redisService.GetItemAsync<ProductDto>(RedisCacheHelperClass.GetProductCacheKey(Id));

            if(productFromCache is not null)
            {
                Log.ForContext(_className, "ProductService").ForContext(_methodName, "GetByIdAsync").Information("Product retrieved from cache successfully - {0}", productFromCache);
                return GenericResponse<ProductDto>.Success(productFromCache, "Product Fetched Successfully.", System.Net.HttpStatusCode.OK);
            }

            ProductDto? product = await _repositoryContext.Products.Include(x => x.productCategory)
                                        .AsNoTracking()
                                        .Select(x =>
                                                    new ProductDto()
                                                    {
                                                        Id = x.Id,
                                                        Name = x.NormalizedName,
                                                        CategoryName = x.productCategory.NormalizedName,
                                                        CostPrice = x.CostPrice,
                                                        SellingPrice = x.SellingPrice,
                                                        Description = x.Description,
                                                        CurrentCount = x.CurrentCount
                                                    })
                                        .SingleOrDefaultAsync(x => x.Id == Id);

            if(product is null)
            {
                Log.ForContext(_className, "ProductService").ForContext(_methodName, "GetByIdAsync").Information("Product with specified Id does not exist - {Id}", Id);
                return GenericResponse<ProductDto>.Failure(null, "Product does not exist.", System.Net.HttpStatusCode.NotFound);
            }

            var setItemToCache = await _redisService.SetItemAsync<ProductDto>(product, RedisCacheHelperClass.GetProductCacheKey(Id), 18400);

            Log.ForContext(_className, "ProductService").ForContext(_methodName, "GetByIdAsync").Information("Product successfully retrived - {product}. Set item to cache - {cacheResponse}", product, setItemToCache);

            return GenericResponse<ProductDto>.Success(product, "Product Fetched Successfully.", System.Net.HttpStatusCode.OK);

        }
        catch(DbUpdateException ex)
        {
            Log.ForContext(_className, "ProductService").ForContext(_methodName, "GetByIdAsync").Error(ex, "Error Retrieving Product from Database.");
            return GenericResponse<ProductDto>.Failure(null, "Error Retrieving Product from Database.", System.Net.HttpStatusCode.InternalServerError, new { Message = ex.Message });
        }
        catch (Exception ex)
        {
            Log.ForContext(_className, "ProductService").ForContext(_methodName, "GetByIdAsync").Error(ex, "Error Occurred Retrieving Product.");
            return GenericResponse<ProductDto>.Failure(null, "Error Occurred Retrieving Product.", System.Net.HttpStatusCode.InternalServerError, new { Message = ex.Message });
        }
    }

    public async Task<GenericResponse<ProductDto>> UpdateAsync(UpdateProductDto updatedProduct)
    {
        try
        {
            Log.ForContext(_className, "ProductService").ForContext(_methodName, "UpdateAsync").Information("Update Product details - {productDetails}", JsonSerializer.Serialize(updatedProduct));

            Product? productToUpdate = await _repositoryContext.Products.IgnoreQueryFilters().SingleOrDefaultAsync(x => x.Id == updatedProduct.Id);

            if(productToUpdate is null)
            {
                Log.ForContext(_className, "ProductService").ForContext(_methodName, "UpdateAsync").Information("Product with Id does not exist - {Id}", updatedProduct.Id);
                return GenericResponse<ProductDto>.Failure(null, $"Product with Id; {updatedProduct.Id} does not exist.", System.Net.HttpStatusCode.NotFound);
            }

            if(productToUpdate.ProductCategoryId != updatedProduct.CategoryId)
            {
                
                bool isProductCategoryExist = await _repositoryContext.ProductCategories.AnyAsync(x => x.Id == updatedProduct.CategoryId);

                if(!isProductCategoryExist)
                {
                    Log.ForContext(_className, "ProductService").ForContext(_methodName, "UpdateAsync").Information("Product Update could not be completed. Product Category does not exist - {CategoryId}", updatedProduct.CategoryId);
                    return GenericResponse<ProductDto>.Failure(null, "Product Category does not exist", System.Net.HttpStatusCode.NotFound);
                }
            }

            productToUpdate.CostPrice = updatedProduct.CostPrice;
            productToUpdate.SellingPrice = updatedProduct.SellingPrice;
            productToUpdate.CurrentCount = updatedProduct.CurrentCount;
            productToUpdate.Description = updatedProduct.Description ?? "";
            productToUpdate.Name = updatedProduct.Name;
            productToUpdate.ProductCategoryId = updatedProduct.CategoryId;
            productToUpdate.IsActive = true;

            //_repositoryContext.Update(productToUpdate);

            await _repositoryContext.SaveChangesAsync();

            ProductDto productDetails = new ProductDto()
                                                        {
                                                            Id = productToUpdate.Id,
                                                            Name = productToUpdate.NormalizedName,
                                                            CategoryName = productToUpdate?.productCategory?.NormalizedName ?? "",
                                                            CostPrice = productToUpdate.CostPrice,
                                                            SellingPrice = productToUpdate.SellingPrice,
                                                            Description = productToUpdate.Description,
                                                            CurrentCount = productToUpdate.CurrentCount
                                                        };

            Log.ForContext(_className, "ProductService").ForContext(_methodName, "UpdateAsync").Information("Product Details Successfully updated - {product}", JsonSerializer.Serialize(productDetails));

            return GenericResponse<ProductDto>.Success(productDetails, "Product Updated Successfully.", System.Net.HttpStatusCode.OK);

        }
        catch(DbUpdateException ex)
        {
            Log.ForContext(_className, "ProductService").ForContext(_methodName, "UpdateAsync").Error(ex, "Database error occurred updating product details.");
            return GenericResponse<ProductDto>.Failure(null, "Database error occurred updating product details.", System.Net.HttpStatusCode.InternalServerError, new { Message = ex.Message });
        }
        catch (Exception ex)
        {
            Log.ForContext(_className, "ProductService").ForContext(_methodName, "UpdateAsync").Error(ex, "An error occurred updating product details.");
            return GenericResponse<ProductDto>.Failure(null, "An error occurred updating product details.", System.Net.HttpStatusCode.InternalServerError, new { Message = ex.Message });
        }
    }

    public async Task<GenericResponse<string>> UpdateStockAsync(UpdateProductStockDto productStock)
    {
        try
        {
            Log.ForContext(_className, "ProductService").ForContext(_methodName, "UpdateStockAsync").Information($"Update Stock Request - {0}", JsonSerializer.Serialize(productStock));

            Product? productToUpdateStock = await _repositoryContext.Products.FindAsync(productStock.Id);

            if(productToUpdateStock is null)
            {
                Log.ForContext(_className, "ProductService").ForContext(_methodName, "UpdateStockAsync").Information($"Product with Id - {0} does not exist.", productStock.Id);
                return GenericResponse<string>.Failure("Operation Failed", "No product with specified id exists", System.Net.HttpStatusCode.NotFound);
            }

            productToUpdateStock.CurrentCount += productStock.StockToAdd;

            await _repositoryContext.SaveChangesAsync();

            var removeFromCcahe = await _redisService.RemoveMultiple(RedisCacheHelperClass.ProductsKey, RedisCacheHelperClass.GetProductCacheKey(productStock.Id));

            Log.ForContext(_className, "ProductService").ForContext(_methodName, "UpdateStockAsync").Information($"Product with Id: {0} Stock Updated Successfully. Current Count - {1}. Remove From cache returns - {2}", productToUpdateStock.Id, productToUpdateStock.CurrentCount, removeFromCcahe);

            return GenericResponse<string>.Success("Operation Successful", $"Product Stock successfully updated. Current Count: {productToUpdateStock.CurrentCount}", System.Net.HttpStatusCode.OK);

        }
        catch(DbException ex)
        {
            Log.ForContext(_className, "ProductService").ForContext(_methodName, "UpdateStockAsync").Error(ex, "A Database Error Occurred Updating Product Stock.");
            return GenericResponse<string>.Failure(null, "Error Occurred Updating Stock.", System.Net.HttpStatusCode.InternalServerError, new { Message = ex.Message });
        }
        catch (Exception ex)
        {
            Log.ForContext(_className, "ProductService").ForContext(_methodName, "UpdateStockAsync").Error(ex, "A Database Error Occurred Updating Product Stock.");
            return GenericResponse<string>.Failure(null, "Error Occurred Updating Stock.", System.Net.HttpStatusCode.InternalServerError, new { Message = ex.Message });
        }
    }

    public async Task<GenericResponse<UpdateProductDto>> GetProductUpdateDetails(int Id)
    {
        try
        {
            Log.ForContext(_className, "ProductService").ForContext(_methodName, "GetProductUpdateDetails").Information("Getting Product Details - {0}", Id);

            UpdateProductDto? productUpdateDetails = await _repositoryContext.Products
                                                        .Select(x => new UpdateProductDto()
                                                        {
                                                            CategoryId = x.ProductCategoryId,
                                                            Name = x.NormalizedName,
                                                            Description = x.Description,
                                                            CostPrice = x.CostPrice,
                                                            SellingPrice = x.SellingPrice,
                                                            Id = x.Id,
                                                            CurrentCount = x.CurrentCount

                                                        })
                                                        .SingleOrDefaultAsync(x => x.Id == Id);

            if(productUpdateDetails is null)
            {
                Log.ForContext(_className, "ProductService").ForContext(_methodName, "GetProductUpdateDetails").Information("No product with Id - {0}", Id);
                return GenericResponse<UpdateProductDto>.Failure(null, $"Product Details with Id - {Id} does not exist.", System.Net.HttpStatusCode.NotFound);
            }

            Log.ForContext(_className, "ProductService").ForContext(_methodName, "GetProductUpdateDetails").Information("Product deails with Id: {0} fetched successfully - {1}", Id, JsonSerializer.Serialize(productUpdateDetails));

            return GenericResponse<UpdateProductDto>.Success(productUpdateDetails, "Product Details fetched successfully.", System.Net.HttpStatusCode.OK);

        }
        catch(DbException ex)
        {
            Log.ForContext(_className, "ProductService").ForContext(_methodName, "GetProductUpdateDetails").Error(ex, "An Error Occurred Fetching Product.");
            return GenericResponse<UpdateProductDto>.Failure(null, "An Error Occurred Fetching Product from database.", System.Net.HttpStatusCode.InternalServerError, new { Message = ex.Message });
        }
        catch (Exception ex)
        {
            Log.ForContext(_className, "ProductService").ForContext(_methodName, "GetProductUpdateDetails").Error(ex, "An Error Occurred Fetching Product from database.");
            return GenericResponse<UpdateProductDto>.Failure(null, "An Error Occurred Fetching Product Details.", System.Net.HttpStatusCode.InternalServerError, new { Message = ex.Message });
        }
    }

    public async Task<GenericResponse<IEnumerable<ProductDto>>> GetMultipleProductsAsync(List<int> Ids)
    {
        try
        {
            Log.ForContext(_className, "ProductService").ForContext(_methodName, "GetMultipleProductsAsync").Information("Get Products with Id in - {0}", JsonSerializer.Serialize(Ids));

            List<ProductDto> products = await _repositoryContext.Products.Where(x => Ids.Contains(x.Id))
                                                    .Select(x => new ProductDto()
                                                    {
                                                        Id = x.Id,
                                                        Name = x.NormalizedName,
                                                        CategoryName = x.productCategory.NormalizedName,
                                                        CostPrice = x.CostPrice,
                                                        SellingPrice = x.SellingPrice,
                                                        Description = x.Description,
                                                        CurrentCount = x.CurrentCount
                                                    })
                                                    .ToListAsync();

            Log.ForContext(_className, "ProductService").ForContext(_methodName, "GetMultipleProductsAsync").Information("Products that Ids is in: {0} - {1}", JsonSerializer.Serialize(Ids), JsonSerializer.Serialize(products));


            return products.Any() ? GenericResponse<IEnumerable<ProductDto>>.Success(products, "Products Fetchedd Successfully", System.Net.HttpStatusCode.OK) : GenericResponse<IEnumerable<ProductDto>>.Failure(null, "Products with Ids does not exist.", System.Net.HttpStatusCode.NotFound);
        }
        catch(DbException ex)
        {
            Log.ForContext(_className, "ProductService").ForContext(_methodName, "GetMultipleProductsAsync").Error(ex, "Error Fetching details from database.");
            return GenericResponse<IEnumerable<ProductDto>>.Failure(null, "An Error Occurred Fetching details from database", System.Net.HttpStatusCode.InternalServerError, new { Message = ex.Message });
        }
        catch (Exception ex)
        {
            Log.ForContext(_className, "ProductService").ForContext(_methodName, "GetMultipleProductsAsync").Error(ex, "Error Fetching details..");
            return GenericResponse<IEnumerable<ProductDto>>.Failure(null, "An Error Occurred Fetching details.", System.Net.HttpStatusCode.InternalServerError, new { Message = ex.Message });
        }
    }
}
