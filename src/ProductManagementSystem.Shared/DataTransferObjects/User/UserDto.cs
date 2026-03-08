namespace ProductManagementSystem.Shared.DataTransferObjects.User;

public record class UserDto
{
    public int Id { get; set; }
    public string FullName { get; set; }
    public string Email { get; set; }
    public string PhoneNumber { get; set; }
    public string AssignedRole { get; set; }
    public bool ActiveStatus { get; set; }
    public bool ConfirmationStatus { get; set; }
    public string Address { get; set; }
}
