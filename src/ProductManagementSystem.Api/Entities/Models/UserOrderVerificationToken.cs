namespace ProductManagementSystem.Api.Entities.Models;

public class UserOrderVerificationToken
{
    public Guid Id { get; set; }
    public string VerificationToken { get; set; }
    public DateTime CreatedAt { get; set; }

    //FOREIGN KEY
    public int OrderId { get; set; }
    public Order OrderToConfirm { get; set; }
}
