namespace ProductManagementSystem.Api.Entities.Models;

public class Feedback
{
    public int Id { get; set; }
    public string Message { get; set; }
    public string Name { get; set; }
    public string UserEmail { get; set; }

    //FEEDBACK-USER RELATION
    public int? UserId { get; set; }
    public User UserProvidingFeedback { get; set; }
}
