using ProductManagementSystem.Shared.DataTransferObjects.ProductCategory;
using ProductManagementSystem.Shared.DataTransferObjects.Response;

namespace ProductManagementSystem.Api.Services.Contracts;

public interface IProductCategoryService
{
    Task<GenericResponse<ProductCategoryDto>> CreateAsync(string productName);
    Task<GenericResponse<IEnumerable<ProductCategoryDto>>> GetAllAsync();
    Task<GenericResponse<ProductCategoryDto>> GetByIdAsync(int categoryId);
    Task<GenericResponse<string>> DeleteAsync(int categoryId);
    Task<GenericResponse<ProductCategoryDto>> UpdateAsync(UpdateProductCategoryDto updateProductCategory);
}
