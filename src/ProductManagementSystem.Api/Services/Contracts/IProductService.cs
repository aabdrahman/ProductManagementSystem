using ProductManagementSystem.Shared.DataTransferObjects.Product;
using ProductManagementSystem.Shared.DataTransferObjects.Response;

namespace ProductManagementSystem.Api.Services.Contracts;

public interface IProductService
{
    Task<GenericResponse<ProductDto>> GetByIdAsync(int Id);
    Task<GenericResponse<ProductDto>> CreateAsync(CreateProductDto productToCreate);
    Task<GenericResponse<IEnumerable<ProductDto>>> GetAllAsync();
    Task<GenericResponse<IEnumerable<ProductDto>>> GetByCategoryIdAsync(int CategoryId);
    Task<GenericResponse<ProductDto>> UpdateAsync(UpdateProductDto updatedProduct);
    Task<GenericResponse<string>> DeleteAsync(int Id, bool isSoftDelete = true);
    Task<GenericResponse<string>> UpdateStockAsync(UpdateProductStockDto productStock);
    Task<GenericResponse<UpdateProductDto>> GetProductUpdateDetails(int Id);
    Task<GenericResponse<IEnumerable<ProductDto>>> GetMultipleProductsAsync(List<int> Ids);
}
