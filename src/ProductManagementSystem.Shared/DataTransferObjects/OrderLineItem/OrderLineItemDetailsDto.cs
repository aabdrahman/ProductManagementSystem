namespace ProductManagementSystem.Shared.DataTransferObjects.OrderLineItem;

public record class OrderLineItemDetailsDto
{
    public int Id { get; set; }
    public string ProductName { get; set; }
    public int OrderCount { get; set; }
    public bool IsActive { get; set; }
}