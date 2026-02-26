namespace ProductManagementSystem.Api.Entities.Models;

public class Review
{
    public int Id { get; set; }
    public int Rating { get; set; }
    public string ReviewText { get; set; }
    public string ReviewerName { get; set; }
    public string? ProductName { get; set; }
}
