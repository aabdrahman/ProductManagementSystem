namespace ProductManagementSystem.Api.Entities.Models;

public class Role
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string NormalizedName { get; set; }

    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; } = true;

    //RELATIONSHIP
    public int? UserId { get; set; }
    public User? CreatedByUser { get; set; }
    //ROLE
    public ICollection<User> Users { get; set; } = [];
}
