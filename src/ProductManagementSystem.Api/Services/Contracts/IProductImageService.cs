using Microsoft.AspNetCore.Components.Forms;

namespace ProductManagementSystem.Api.Services.Contracts;

public interface IProductImageService
{
    Task<bool> AddProductImage(int productId, List<IFormFile> productImages);
    Task<bool> RemoveProductImage(int productId, string productImageName);
    Task<FileStream> GetProductImage(int productId, string productImageName);
}
