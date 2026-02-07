namespace ProductManagementSystem.Api.Entities.Models;

public class Order
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; }
    public int OrderCount { get; set; }

    //RELATIONSHIP
    //Product
    public Product OrderedProduct { get; set; }
    public int ProductId { get; set; }
}
