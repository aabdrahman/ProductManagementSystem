namespace ProductManagementSystem.Shared.DataTransferObjects.RequestParameters;

public class OrderRequestParameters
{
    public DateOnly StartDate { get; set; } = DateOnly.FromDateTime(DateTime.Now.AddDays(-7));
    public DateOnly EndDate { get; set; } = DateOnly.FromDateTime(DateTime.Now);
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 15;
    public int? ProductId { get; set; }
    public string? OrderStatus { get; set; }
    public string? TrackingNumber { get; set; }
}
