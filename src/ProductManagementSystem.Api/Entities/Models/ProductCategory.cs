namespace ProductManagementSystem.Api.Entities.Models;

public class ProductCategory
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string NormalizedName { get; set; }

    //RELATIONSHIPS
    public ICollection<Product> Products { get; set; } = [];
}
