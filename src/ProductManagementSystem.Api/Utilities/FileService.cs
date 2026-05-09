using Microsoft.AspNetCore.Components.Forms;
using ProductManagementSystem.Api.Data;
using ProductManagementSystem.Api.Entities.Models;
using ProductManagementSystem.Api.Utilities.Contracts;
using Serilog;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace ProductManagementSystem.Api.Utilities;

public class FileService : IFileService
{
    private string _productImagePath;
    private readonly RepositoryContext _repositoryContext;
    private Serilog.ILogger logger;

    private string _methodName = "MethodName";
    private string _className = "ClassName";

    public FileService(RepositoryContext repositoryContext)
    {
        _productImagePath = Path.Combine(Directory.GetCurrentDirectory(), "StaticFiles", "ProductImages");
        _repositoryContext = repositoryContext;

        logger = Log.ForContext(_className, nameof(FileService));
    }

    public Task<IEnumerable<IBrowserFile>> GetFilesFromPath(int productId)
    {
        throw new NotImplementedException();
    }

    public async Task<bool> WriteToPath(int productId, List<IFormFile> productImages)
    {
        if(!Path.Exists(_productImagePath))
        {
            Directory.CreateDirectory(_productImagePath);
        }

        string specificProductImagePath = Path.Combine(_productImagePath, productId.ToString());

        if (!Directory.Exists(specificProductImagePath))
        {
            Directory.CreateDirectory(specificProductImagePath);
        }

        var resizeOption = new ResizeOptions()
        {
            Mode = ResizeMode.Pad,
            Size = new Size(width: 250, height: 250)
        };

        List<string> savedImages = [];

        foreach (var image in productImages)
        {
            var fileName = image.FileName;
            var extension = Path.GetExtension(fileName).ToLowerInvariant();

            using var imageOperator = await Image.LoadAsync(image.OpenReadStream());
            imageOperator.Mutate(x => x.Resize(resizeOption));

            string saveFilename = string.Concat(Guid.NewGuid().ToString(), extension);

            var saveLocation = Path.Combine(specificProductImagePath, saveFilename);

            switch (extension)
            {
                case ".png":
                    await imageOperator.SaveAsPngAsync(saveLocation);
                    break;
                case ".jpg":
                    await imageOperator.SaveAsJpegAsync(saveLocation);
                    break;
                case ".jpeg":
                    await imageOperator.SaveAsJpegAsync(saveLocation);
                    break;

                default:
                    await imageOperator.SaveAsJpegAsync(saveLocation);
                    break;
            }

            savedImages.Add(saveFilename);

            ProductImage imageToInsert = new ProductImage()
            {
                CreatedAt = DateTime.UtcNow,
                Filename = saveFilename,
                ProductId = productId
            };

            await _repositoryContext.ProductImages.AddAsync(imageToInsert);
        }

        try
        {
            await _repositoryContext.SaveChangesAsync();

            return true;
        }
        catch (Exception ex)
        {
            foreach (var item in savedImages)
            {
                File.Delete(Path.Combine(specificProductImagePath, item));
            }
            return false;
        }
    }

    public async Task<bool> RemoveProductImage(int productId, string fileName)
    {
        var logProvider = logger.ForContext(_methodName, nameof(RemoveProductImage));

        try
        {
            logProvider.Information("Remove product image: {0} from product - {1}", fileName, productId);

            var filePath = Path.Combine(_productImagePath, productId.ToString(), fileName);

            File.Delete(filePath);

            logProvider.Information("File removed successfully. File name - {0}", fileName);

            return true;

        }
        catch (Exception ex)
        {
            logProvider.Error(ex, "An error occurred removing file from path.");

            return false;
        }
        throw new NotImplementedException();
    }

    public Task<FileStream> GetFilesAsync(int productId, string fileName)
    {
        var logProvider = logger.ForContext(_methodName, nameof(GetFilesAsync));
        try
        {
            var filePath = Path.Combine(_productImagePath, productId.ToString(), fileName); 

            return Task.FromResult(File.OpenRead(filePath));
        }
        catch (Exception ex)
        {
            logProvider.Error(ex, "An error occurred reading file.");
            return default;
        }

        throw new NotImplementedException();
    }
}
