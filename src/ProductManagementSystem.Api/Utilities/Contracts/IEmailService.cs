namespace ProductManagementSystem.Api.Utilities.Contracts;

public interface IEmailService
{
    Task<bool> SendEmailAsync(string email);
    Task<bool> RevokeEmailAsync(string email);
}
