namespace ProductManagementSystem.Api.Utilities.Contracts;

public interface IPasswordHasher
{
    string HashPassword(string password);
    bool ValidatePassword(string hashedPassword, string passwordToValidate);
}
