namespace ProductManagementSystem.Api.Entities.Models;

public class OrderLineItem
{
    public int Id { get; set; }
    public bool IsActive { get; set; } = true;
    public int QuantityOrdered { get; set; }

    //Relationship
    public int OrderId { get; set; }
    public Order order { get; set; }

    public int ProductId { get; set; }
    public Product OrderedProduct { get; set; }
}