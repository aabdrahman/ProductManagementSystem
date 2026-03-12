namespace ProductManagementSystem.Shared.DataTransferObjects.Feedback;

public record class FeedbackDto
{
    public string Name { get; set; }
    public string Message { get; set; }
    public string EmailAddress { get; set; }
    public DateTime CreatedAt { get; set; }
}
