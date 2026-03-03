using ProductManagementSystem.Api.Entities.StaticValues;

namespace ProductManagementSystem.Api.Entities.Models;

public class Order
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; }
    public int OrderCount { get; set; }
    public DateTime? DeliveryDate { get; set; }
    public OrderStatus OrderStatus { get; set; }
    public bool IsActive { get; set; }
    public string OrderTrackingId { get; set; }

    //RELATIONSHIP
    //Product
    public Product OrderedProduct { get; set; }
    public int ProductId { get; set; }
}


public class OrderLineItem
{
    public int Id { get; set; }

    //Relationship
    public int OrderId { get; set; }
    public Order order { get; set; }

    public int ProductId { get; set; }
    public Product OrderedProduct { get; set; }
}