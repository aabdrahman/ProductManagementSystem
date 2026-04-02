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
    public DateTime? LastLoginDate { get; set; }
    public DateTime? LastAuthenticatedDate { get; set; }
    public DateTime? LastUpdatedAt { get; set; }
    public bool IsProfileLockedOut { get; set; } = false;


    //RELATIONSHIPS
    //ORDERS
    public ICollection<Order> Orders { get; set; } = [];
    public ICollection<Feedback> UserFeedbacks { get; set; } = [];

    //ROLE
    public int RoleId { get; set; }
    public Role AssignedRole { get; set; }
}
