using ProductManagementSystem.Api.Entities.StaticValues;

namespace ProductManagementSystem.Api.Entities.Models;

public class Order
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; }
    public DateTime? DeliveryDate { get; set; }
    public OrderStatus OrderStatus { get; set; }
    public bool IsActive { get; set; }
    public string OrderTrackingId { get; set; }
    public string DeliveryAddress { get; set; }
    public bool IsConfirmed { get; set; } = false;

    //RELATIONSHIP
    //Product
    //public Product OrderedProduct { get; set; }
    //public int ProductId { get; set; }

    //Order Line Items
    public ICollection<OrderLineItem> OrderLineItems { get; set; } = [];

    //User Order Verification
    public ICollection<UserOrderVerificationToken> UserOrderVerificationTokens { get; set; } = [];

    //User Created By
    public int? UserId { get; set; }
    public User CreatedByUser { get; set; }
}
