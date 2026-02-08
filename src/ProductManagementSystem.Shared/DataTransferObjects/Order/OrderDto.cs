namespace ProductManagementSystem.Shared.DataTransferObjects.Order;

public record class OrderDto
{
    public int Id { get; set; }
    public int QuantityOrdered { get; set; }
    public string OrderStatus { get; set; }
    public string Product { get; set; }
    public DateTime CreatedDate { get; set; }
    public string CreatedBy { get; set; }
}
