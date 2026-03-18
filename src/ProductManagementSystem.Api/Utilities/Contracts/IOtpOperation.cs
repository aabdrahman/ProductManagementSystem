namespace ProductManagementSystem.Api.Utilities.Contracts;

public interface IOtpOperation
{
    string GenerateOtp(int length = 6);
}
