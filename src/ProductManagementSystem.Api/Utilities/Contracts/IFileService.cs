using Microsoft.AspNetCore.Components.Forms;

namespace ProductManagementSystem.Api.Utilities.Contracts;

public interface IFileService
{
    Task<bool> WriteToPath(int productId, List<IFormFile> productImages);
    Task<IEnumerable<IBrowserFile>> GetFilesFromPath(int productId);
    Task<bool> RemoveProductImage(int productId, string fileName);
    Task<FileStream> GetFilesAsync(int productId, string fileName);
}
