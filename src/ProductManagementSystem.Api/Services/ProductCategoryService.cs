using Microsoft.EntityFrameworkCore;
using ProductManagementSystem.Api.Data;
using ProductManagementSystem.Api.Entities.ChannelBrokers;
using ProductManagementSystem.Api.Entities.Models;
using ProductManagementSystem.Api.Helpers;
using ProductManagementSystem.Api.Services.Contracts;
using ProductManagementSystem.Api.Utilities.Contracts;
using ProductManagementSystem.Shared.DataTransferObjects.ProductCategory;
using ProductManagementSystem.Shared.DataTransferObjects.Response;
using Serilog;
using System.Text.Json;
using System.Threading.Channels;

namespace ProductManagementSystem.Api.Services;

public sealed class ProductCategoryService : IProductCategoryService
{
    private RepositoryContext _repositoryContext;
    private readonly Channel<CacheItemProcessor<CacheItem>> _channel;
    private readonly IRedisService _redisService;
    private string _methodName = "MethodName";
    private string _className = "ClassName";
    public ProductCategoryService(RepositoryContext repositoryContext, Channel<CacheItemProcessor<CacheItem>> channel, IRedisService redisService)
    {
        _repositoryContext = repositoryContext;
        _channel = channel;
        _redisService = redisService;
    }

    public async Task<GenericResponse<ProductCategoryDto>> CreateAsync(string productName)
    {
        try
        {
            Log.ForContext(_className, "ProductCategoryService").ForContext(_methodName, "CreateAsync").Information($"Creating Product Category - {productName}");

            bool isAlreadyExists = await _repositoryContext.ProductCategories.AsNoTracking().AnyAsync(x => x.NormalizedName == productName.ToUpper());

            if (isAlreadyExists)
            {
                Log.ForContext(_className, "ProductCategoryService").ForContext(_methodName, "CreateAsync").Information($"Creating Product Category Failed. Already Exists - {productName}");
                return GenericResponse<ProductCategoryDto>.Failure(null, $"Product Category already exists: {productName}", System.Net.HttpStatusCode.Conflict);
            }

            ProductCategory productCategoryToInsert = new ProductCategory() 
            {
                Name = productName
            };

            await _repositoryContext.ProductCategories.AddAsync(productCategoryToInsert);

            await _repositoryContext.SaveChangesAsync();

            ProductCategoryDto productCategoryToReturn = new ProductCategoryDto() { Id = productCategoryToInsert.Id, Name = productCategoryToInsert.NormalizedName };

            var cacheItemResult = await _redisService.SetItemAsync<ProductCategoryDto>(productCategoryToReturn, RedisCacheHelperClass.GetProductCategoryItemKey(productCategoryToReturn.Id), 18400);

            var removeCacheResult = await _redisService.RemoveItemAsync(RedisCacheHelperClass.ProductCategoryKey);


            Log.ForContext(_className, "ProductCategoryService").ForContext(_methodName, "CreateAsync").Information("Product Category Successfully Created - {0}. Cache Item Result - {1}. Remove Cached Items result - {2}", 
                                                                        productCategoryToReturn, cacheItemResult, removeCacheResult);

            return GenericResponse<ProductCategoryDto>.Success(productCategoryToReturn, "Product Category Created.", System.Net.HttpStatusCode.OK);

        }
        catch(DbUpdateException ex)
        {
            Log.ForContext(_className, "ProductCategoryService").ForContext(_methodName, "CreateAsync").Error(ex, $"Database Error Creating Product Category");
            return GenericResponse<ProductCategoryDto>.Failure(null, $"Product Category Creation Failed: {productName}", System.Net.HttpStatusCode.InternalServerError, new { Message = ex.Message });
        }
        catch (Exception ex)
        {
            Log.ForContext(_className, "ProductCategoryService").ForContext(_methodName, "CreateAsync").Error(ex, "Database Error Creating Product Category");
            return GenericResponse<ProductCategoryDto>.Failure(null, $"Product Category Creation Failed: {productName}", System.Net.HttpStatusCode.InternalServerError, new { Message = ex.Message });
        }
    }

    public async Task<GenericResponse<string>> DeleteAsync(int categoryId)
    {
        try
        {
            Log.ForContext(_className, "ProductCategoryService").ForContext(_methodName, "DeleteAsync").Information($"Deleting Product Category - {categoryId}");

            ProductCategory? productCategoryToRemove = await _repositoryContext.ProductCategories.FindAsync(categoryId);

            if(productCategoryToRemove == null)
            {
                Log.ForContext(_className, "ProductCategoryService").ForContext(_methodName, "DeleteAsync").Information($"Product Category does not exist - {categoryId}");
                return GenericResponse<string>.Failure("Operation Failed", $"Product Category with Id: {categoryId} does not exist.", System.Net.HttpStatusCode.NotFound);
            }

            bool existProductLinked = await _repositoryContext.Products.AnyAsync(x => x.ProductCategoryId == categoryId);

            if(existProductLinked)
            {
                Log.ForContext(_className, "ProductCategoryService").ForContext(_methodName, "DeleteAsync").Information($"Product Category has one or more products linked - {categoryId}");
                return GenericResponse<string>.Failure("Operation Failed", $"Product Category with Id: {categoryId} still has some products linked.", System.Net.HttpStatusCode.Conflict);
            }

            _repositoryContext.ProductCategories.Remove(productCategoryToRemove);

            await _repositoryContext.SaveChangesAsync();

            var deleteFromCacheResult = await _redisService.RemoveMultiple(RedisCacheHelperClass.ProductCategoryKey, RedisCacheHelperClass.GetProductCategoryItemKey(categoryId));

            Log.ForContext(_className, "ProductCategoryService").ForContext(_methodName, "DeleteAsync").Information("Product Category deleted successfully. Cache Removal returns - {0}", deleteFromCacheResult);

            return GenericResponse<string>.Success("Operation Successful", "Product Category Successfully Removed.", System.Net.HttpStatusCode.OK);
        }
        catch(DbUpdateException ex)
        {
            Log.ForContext(_className, "ProductCategoryService").ForContext(_methodName, "DeleteAsync").Error(ex, "Errror Removing Product Category From Database.");
            return GenericResponse<string>.Failure("Operation Failed.", "Errror Removing Product Category From Database.", System.Net.HttpStatusCode.InternalServerError, new { Message = ex.Message });
        }
        catch (Exception ex)
        {
            Log.ForContext(_className, "ProductCategoryService").ForContext(_methodName, "DeleteAsync").Error(ex, "Error Occurred Removing Product Category.");
            return GenericResponse<string>.Failure("Operation Failed.", "Error Occurred Removing Product Category.", System.Net.HttpStatusCode.InternalServerError, new { Message = ex.Message });
        }
    }

    public async Task<GenericResponse<IEnumerable<ProductCategoryDto>>> GetAllAsync()
    {
        try
        {
            Log.ForContext(_className, "ProductCategoryService").ForContext(_methodName, "GetAllAsync").Information($"Fetching All Product Categories");

            var productCategoriesFromCache = await _redisService.GetItemAsync<IEnumerable<ProductCategoryDto>>(RedisCacheHelperClass.ProductCategoryKey);

            if(productCategoriesFromCache != null && productCategoriesFromCache.Any())
            {
                Log.ForContext(_className, "ProductCategoryService").ForContext(_methodName, "GetAllAsync").Information("Product Categories Fetched from cache - {0}", productCategoriesFromCache);

                return GenericResponse<IEnumerable<ProductCategoryDto>>.Success(productCategoriesFromCache, "Product Categories Fetched Successfully.", System.Net.HttpStatusCode.OK);
            }

            List<ProductCategoryDto> productCategories = await _repositoryContext.ProductCategories.AsNoTracking()
                                                .Select( x => new ProductCategoryDto() { Id = x.Id, Name = x.NormalizedName})    
                                                .ToListAsync();

            var cacheItemResult = await _redisService.SetItemAsync<IEnumerable<ProductCategoryDto>>(productCategories, RedisCacheHelperClass.ProductCategoryKey, 18400);

            Log.ForContext(_className, "ProductCategoryService").ForContext(_methodName, "GetAllAsync").Information($"Product Categories Successfully Fetched - {JsonSerializer.Serialize(productCategories)}. Cache Item Result - {cacheItemResult}");

            //await _channel.Writer.WriteAsync(new CacheItemProcessor<CacheItem>(new CacheItem(value: JsonSerializer.Serialize(productCategories), typeof(List<ProductCategoryDto>).ToString(), key: RedisCacheHelperClass.ProductCategoryKey, 18400)));

            return GenericResponse<IEnumerable<ProductCategoryDto>>.Success(productCategories, "Product Categories Fetched Successfully.", System.Net.HttpStatusCode.OK);
        }
        catch(DbUpdateException ex)
        {
            Log.ForContext(_className, "ProductCategoryService").ForContext(_methodName, "GetAllAsync").Error(ex, "Error Retrieving Product Categories From Database");
            return GenericResponse<IEnumerable<ProductCategoryDto>>.Failure(null, "Error Retrieving Product Categories From Database", System.Net.HttpStatusCode.InternalServerError, new { Message = ex.Message });
        }
        catch (Exception ex)
        {
            Log.ForContext(_className, "ProductCategoryService").ForContext(_methodName, "GetAllAsync").Error(ex, "Error Retrieving Product Categories");
            return GenericResponse<IEnumerable<ProductCategoryDto>>.Failure(null, "Error Retrieving Product Categories", System.Net.HttpStatusCode.InternalServerError, new { Message = ex.Message });
        }
    }

    public async Task<GenericResponse<ProductCategoryDto>> GetByIdAsync(int categoryId)
    {
        try
        {
            Log.ForContext(_className, "ProductCategoryService").ForContext(_methodName, "GetByIdAsync").Information($"Fetching Product Category By Id - {categoryId}");

            var productCategoryFromCache = await _redisService.GetItemAsync<ProductCategoryDto>(RedisCacheHelperClass.GetProductCategoryItemKey(categoryId));

            if(productCategoryFromCache is not null)
            {
                Log.ForContext(_className, "ProductCategoryService").ForContext(_methodName, "GetByIdAsync").Information("Product Category Fetched from cache - {0}", productCategoryFromCache);

                return GenericResponse<ProductCategoryDto>.Success(productCategoryFromCache, "Product Category Fetched Successfully.", System.Net.HttpStatusCode.OK);
            }

            ProductCategoryDto? productCategory = await _repositoryContext.ProductCategories
                                                .AsNoTracking()
                                                .Select(x => new ProductCategoryDto() { Id = x.Id, Name = x.NormalizedName})
                                                .SingleOrDefaultAsync(x => x.Id == categoryId);

            if(productCategory is null)
            {
                Log.ForContext(_className, "ProductCategoryService").ForContext(_methodName, "GetByIdAsync").Information($"Product Category with Id: {categoryId} does not exist.");
                return GenericResponse<ProductCategoryDto>.Failure(null, $"Product Category does not exist: {categoryId}", System.Net.HttpStatusCode.NotFound);
            }

            //await _channel.Writer.WriteAsync(new CacheItemProcessor<CacheItem>(new CacheItem(value: JsonSerializer.Serialize(productCategory), type: typeof(ProductCategoryDto).ToString(), key: RedisCacheHelperClass.GetProductCategoryItemKey(productCategory.Id), expiresAfter: 18400)));

            var cacheItemResult = await _redisService.SetItemAsync<ProductCategoryDto>(productCategory, RedisCacheHelperClass.GetProductCategoryItemKey(productCategory.Id), 18400);

            Log.ForContext(_className, "ProductCategoryService").ForContext(_methodName, "GetByIdAsync").Information($"Product Category with Id: {categoryId} fetched Successfully - {JsonSerializer.Serialize(productCategory)}");

            return GenericResponse<ProductCategoryDto>.Success(productCategory, "Product Category Fetched Successfully.", System.Net.HttpStatusCode.OK);

        }
        catch(DbUpdateException ex)
        {
            Log.ForContext(_className, "ProductCategoryService").ForContext(_methodName, "GetByIdAsync").Error(ex, "Error Retrieving Product Category from Database");
            return GenericResponse<ProductCategoryDto>.Failure(null, "Error Retrieving Product Category from Database", System.Net.HttpStatusCode.InternalServerError, new { message = ex.Message });
        }
        catch (Exception ex)
        {
            Log.ForContext(_className, "ProductCategoryService").ForContext(_methodName, "GetByIdAsync").Error(ex, "Error Occurred Retrieving Product Category");
            return GenericResponse<ProductCategoryDto>.Failure(null, "Error Occurred Retrieving Product Category", System.Net.HttpStatusCode.InternalServerError, new { message = ex.Message });
        }
    }

    public async Task<GenericResponse<ProductCategoryDto>> UpdateAsync(UpdateProductCategoryDto updateProductCategory)
    {
        try
        {
            Log.ForContext(_className, "ProductCategoryService").ForContext(_methodName, "UpdateAsync").Information($"Updating Product Category Details - {JsonSerializer.Serialize(updateProductCategory)}");

            ProductCategory? productToUpdate = await _repositoryContext.ProductCategories.SingleOrDefaultAsync(x => x.Id == updateProductCategory.Id);

            if(productToUpdate is null)
            {
                Log.ForContext(_className, "ProductCategoryService").ForContext(_methodName, "UpdateAsync").Information($"Product Category does not exist - {updateProductCategory.Id}");
                return GenericResponse<ProductCategoryDto>.Failure(null, "", System.Net.HttpStatusCode.NotFound);
            }

            productToUpdate.Name = updateProductCategory.Name;

           //_repositoryContext.ProductCategories.Update(productToUpdate);

            await _repositoryContext.SaveChangesAsync();

            var deleteFromCacheResult = await _redisService.RemoveMultiple(RedisCacheHelperClass.ProductCategoryKey, RedisCacheHelperClass.GetProductCategoryItemKey(productToUpdate.Id));

            Log.ForContext(_className, "ProductCategoryService").ForContext(_methodName, "UpdateAsync").Information($"Product Category SUccessfully Updated - {JsonSerializer.Serialize(new ProductCategoryDto() { Id = productToUpdate.Id, Name = productToUpdate.NormalizedName })}. Cache Removal returns - {deleteFromCacheResult}");

            return GenericResponse<ProductCategoryDto>.Success(new ProductCategoryDto() { Id = productToUpdate.Id, Name = productToUpdate.NormalizedName }, "Product Category Successfully Updated.", System.Net.HttpStatusCode.OK);
        }
        catch(DbUpdateException ex)
        {
            Log.ForContext(_className, "ProductCategoryService").ForContext(_methodName, "UpdateAsync").Error(ex, "Database Error Occurred Updating Product Category");
            return GenericResponse<ProductCategoryDto>.Failure(null, "Database Error Occurred Updating Product Category", System.Net.HttpStatusCode.InternalServerError, new { Message = ex.Message });
        }
        catch (Exception ex)
        {
            Log.ForContext(_className, "ProductCategoryService").ForContext(_methodName, "UpdateAsync").Error(ex, "Error Occurred Updating Product Category");
            return GenericResponse<ProductCategoryDto>.Failure(null, "Error Occurred Updating Product Category", System.Net.HttpStatusCode.InternalServerError, new { Message = ex.Message });
        }
    }
}
