namespace ProductManagementSystem.Api.Entities.Models;

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string NormalizedName { get; set; }
    public int CurrentCount { get; set; }
    public string Description { get; set; }
    public decimal CostPrice { get; set; }
    public decimal SellingPrice { get; set; }
    public bool IsActive { get; set; } = true;
    //RELATIONSHIPS
    //Product Category
    public int ProductCategoryId { get; set; }
    public ProductCategory productCategory { get; set; }

    //Order
    public ICollection<Order> Orders { get; set; }
}
