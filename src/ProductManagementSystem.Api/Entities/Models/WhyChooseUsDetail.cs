namespace ProductManagementSystem.Api.Entities.Models;

public class WhyChooseUsDetail
{
    public Guid Id { get; set; }
    public string Title { get; set; }
    public string Content { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; }
    public bool IsDeleted { get; set; } = false;
    public string? DeletedBy { get; set; }
    public DateTime? DeletedAt { get; set; }

}
