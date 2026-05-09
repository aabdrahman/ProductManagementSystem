using Microsoft.AspNetCore.Components.Forms;
using Microsoft.EntityFrameworkCore;
using ProductManagementSystem.Api.Data;
using ProductManagementSystem.Api.Helpers;
using ProductManagementSystem.Api.Services.Contracts;
using ProductManagementSystem.Api.Utilities.Contracts;
using Serilog;

namespace ProductManagementSystem.Api.Services;

public class ProductImageService : IProductImageService
{
    private readonly IFileService _fileService;
    private readonly RepositoryContext _repositoryContext;
    private readonly IRedisService _redisService;

    private string _methodName = "MethodName";
    private string _className = "ClassName";

    private readonly Serilog.ILogger logger;
    public ProductImageService(IFileService fileService, RepositoryContext repositoryContext, IRedisService redisService)
    {
        _fileService = fileService;
        logger = Log.ForContext(_className, nameof(ProductImageService));
        _repositoryContext = repositoryContext;
        _redisService = redisService;
    }
    public async Task<bool> AddProductImage(int productId, List<IFormFile> productImages)
    {
        var logProvider = logger.ForContext(_methodName, nameof(AddProductImage));
        try
        {
            logProvider.Information("Add Product Image for - {0}", productId);

            bool productExists = await _repositoryContext.Products.AnyAsync(x => x.Id == productId);

            if (!productExists)
            {
                logProvider.Information("Product with Id - {0} does not exist.", productId);
                return false;
            }

            var isProcessed = await _fileService.WriteToPath(productId, productImages);
            
            var removeFromCache = await _redisService.RemoveMultiple(RedisCacheHelperClass.ProductsKey, RedisCacheHelperClass.GetProductCacheKey(productId));

            logProvider.Information("Product Image files processor returns - {0}. Cache Removal returns: {1}", isProcessed, removeFromCache);

            if (!isProcessed)
            {
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            logProvider.Error(ex, "An error occrred processing product image files.");

            return false;
        }
    }

    public async Task<bool> RemoveProductImage(int productId, string productImageName)
    {
        var logProvider = logger.ForContext(_methodName, nameof(RemoveProductImage));

        try
        {
            logProvider.Information("Removing product image: {0} from product - {1}", productImageName, productId);

            var productImageToRemove = await _repositoryContext.ProductImages.FirstOrDefaultAsync(x => x.ProductId == productId && x.Filename == productImageName);

            if(productImageToRemove is null)
            {
                logProvider.Information("Product with specified details does not exist.");

                return false;
            }

            var removeFromPath = await _fileService.RemoveProductImage(productId, productImageName);

            if (!removeFromPath)
            {
                logProvider.Information("Product Image removal from path failed. Returns - {0}", removeFromPath);
                return false;
            }

            _repositoryContext.ProductImages.Remove(productImageToRemove);

            await _repositoryContext.SaveChangesAsync();

            var removeFromCache = await _redisService.RemoveMultiple(RedisCacheHelperClass.ProductsKey, RedisCacheHelperClass.GetProductCacheKey(productId));

            logProvider.Information("Product image removal operation successful. Remove from cache returns: {0}", removeFromCache);

            return true;
        }
        catch (Exception ex)
        {
            logProvider.Error(ex, "An error occurred removing product image.");

            return false;
        }
    }

    public Task<FileStream> GetProductImage(int productId, string productImageName)
    {

        return _fileService.GetFilesAsync(productId, productImageName);

        throw new NotImplementedException();
    }
}
