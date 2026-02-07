namespace ProductManagementSystem.Shared.DataTransferObjects.ProductCategory;

public record class UpdateProductCategoryDto
{
    public int Id { get; set; }
    public string Name { get; set; }
}
