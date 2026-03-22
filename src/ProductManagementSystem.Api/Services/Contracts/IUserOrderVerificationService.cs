namespace ProductManagementSystem.Api.Services.Contracts;

public interface IUserOrderVerificationService
{
    Task<string> VerifyOrderAsync(string orderVerificationToken);
}
