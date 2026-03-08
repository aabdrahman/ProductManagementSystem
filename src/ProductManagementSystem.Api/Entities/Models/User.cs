namespace ProductManagementSystem.Api.Entities.Models;

public class User
{
    public int Id { get; set; }
    public required string UserEmailAddress { get; set; }
    public string Address { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string? MiddleName { get; set; }
    public string PhoneNumber { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public bool IsUserConfirmed { get; set; } = false;
    public DateTime? ConfirmedAt { get; set; }
    public string PasswordHash { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiryTime { get; set; }


    //RELATIONSHIPS
    public ICollection<Order> Orders { get; set; } = [];
}
