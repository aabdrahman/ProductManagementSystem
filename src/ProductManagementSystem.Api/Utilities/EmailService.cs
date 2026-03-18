using ProductManagementSystem.Api.Utilities.Contracts;

namespace ProductManagementSystem.Api.Utilities;

public class EmailService : IEmailService
{
    public Task<bool> RevokeEmailAsync(string email)
    {
        throw new NotImplementedException();
    }

    public Task<bool> SendEmailAsync(string email)
    {
        throw new NotImplementedException();
    }
}
