using Microsoft.EntityFrameworkCore;
using ProductManagementSystem.Api.Data;
using ProductManagementSystem.Api.Entities.Models;
using ProductManagementSystem.Api.Services.Contracts;
using ProductManagementSystem.Shared.DataTransferObjects.Product;
using ProductManagementSystem.Shared.DataTransferObjects.Response;
using Serilog;
using System.Text.Json;

namespace ProductManagementSystem.Api.Services;

public sealed class ProductService : IProductService
{
    private string _methodName = "MethodName";
    private string _className = "ClassName";

    private readonly RepositoryContext _repositoryContext;
    public ProductService(RepositoryContext repositoryContext = null)
    {
        _repositoryContext = repositoryContext;
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

            Log.ForContext(_className, "ProductService").ForContext(_methodName, "CreateAsync").Information("Product Creation Successful - {createdProduct}", JsonSerializer.Serialize(createdProduct));

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
                _repositoryContext.Products.Update(productToRemove);
            }
            else
            {
                bool isOrderExists = await _repositoryContext.Orders.AnyAsync(x => x.ProductId == Id && (x.Ordertatus == Entities.StaticValues.OrderStatus.Pending || x.Ordertatus == Entities.StaticValues.OrderStatus.Processing));

                if (isOrderExists)
                {
                    Log.ForContext(_className, "ProductService").ForContext(_methodName, "DeleteAsync").Information("The product to remove has one or more orders still not concluded");
                    return GenericResponse<string>.Failure("Operation Failed", "Pending order exists for selected product to remove.", System.Net.HttpStatusCode.Conflict);
                }

                _repositoryContext.Products.Remove(productToRemove);
            }

            await _repositoryContext.SaveChangesAsync();

            Log.ForContext(_className, "ProductService").ForContext(_methodName, "DeleteAsync").Information((isSoftDelete ? $"Product successfully marked as inactive - {Id}" : $"Product with Id sucessfully removed - {Id}"));

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
            Log.ForContext(_className, "ProductService").ForContext(_methodName, "GetAllAsync").Information("Fetching All Products.....");

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
            Log.ForContext(_className, "ProductService").ForContext(_methodName, "GetByCategoryId").Information("Fetching Product with Id - {Id}", Id);

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
                Log.ForContext(_className, "ProductService").ForContext(_methodName, "GetByCategoryId").Information("Product with specified Id does not exist - {Id}", Id);
                return GenericResponse<ProductDto>.Failure(null, "Product does not exist.", System.Net.HttpStatusCode.NotFound);
            }


            Log.ForContext(_className, "ProductService").ForContext(_methodName, "GetByCategoryId").Information("Product successfully retrived - {product}", product);

            return GenericResponse<ProductDto>.Success(product, "Product Fetched Successfully.", System.Net.HttpStatusCode.OK);

        }
        catch(DbUpdateException ex)
        {
            Log.ForContext(_className, "ProductService").ForContext(_methodName, "GetByCategoryId").Error(ex, "Error Retrieving Product from Database.");
            return GenericResponse<ProductDto>.Failure(null, "Error Retrieving Product from Database.", System.Net.HttpStatusCode.InternalServerError, new { Message = ex.Message });
        }
        catch (Exception ex)
        {
            Log.ForContext(_className, "ProductService").ForContext(_methodName, "GetByCategoryId").Error(ex, "Error Occurred Retrieving Product.");
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

            _repositoryContext.Update(productToUpdate);

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
}
