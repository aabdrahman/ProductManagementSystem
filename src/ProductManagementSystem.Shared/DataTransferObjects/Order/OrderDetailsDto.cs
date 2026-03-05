using ProductManagementSystem.Shared.DataTransferObjects.OrderLineItem;

namespace ProductManagementSystem.Shared.DataTransferObjects.Order;

public record class OrderDetailsDto
{
    public int Id { get; set; }
    public string OrderStatus { get; set; }
    public DateTime CreatedDate { get; set; }
    public string CreatedBy { get; set; }
    public string DeliveryAddress { get; set; }
    public string OrderNumber { get; set; }
    public List<OrderLineItemDetailsDto> OrderLineItems { get; set; }
}
