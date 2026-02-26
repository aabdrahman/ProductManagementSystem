namespace ProductManagementSystem.Shared.DataTransferObjects.Review;

public record class ReviewDto
{
    public int Id { get; set; }
    public string ReviewerName { get; set; } = string.Empty;
    public string ReviewText { get; set; } = string.Empty;
    public int Rating { get; set; } // 1-5 stars
    public string? ProductName { get; set; } // Optional: link review to product
}
