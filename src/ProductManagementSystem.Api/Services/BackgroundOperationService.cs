using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ProductManagementSystem.Api.Data;
using ProductManagementSystem.Api.Entities.ConfigurationModels;
using ProductManagementSystem.Api.Services.Contracts;
using ProductManagementSystem.Shared.DataTransferObjects.Response;
using Serilog;

namespace ProductManagementSystem.Api.Services;

public class BackgroundOperationService : IBackgroundOperationService
{
    private readonly RepositoryContext _repositoryContext;
    private readonly OtpSettingsConfig _otpSettings;
    private readonly UserOrderVerificationConfig _userOrderVerificationConfig;

    public BackgroundOperationService(RepositoryContext repositoryContext, IOptionsMonitor<RemoveExpiredOtpBackgroundConfig> optionsMonitor, 
                                        IOptionsMonitor<OtpSettingsConfig> otpSettingsOptionsMonitor, IOptionsMonitor<UserOrderVerificationConfig> userOrderVerificationOptionsMonitor)
    {
        _repositoryContext = repositoryContext;
        _otpSettings = otpSettingsOptionsMonitor.CurrentValue;
        _userOrderVerificationConfig = userOrderVerificationOptionsMonitor.CurrentValue;
    }

    public async Task<GenericResponse<string>> RemoveExpiredOTPAsync()
    {
        try
        {
            Log.ForContext("ClassName", nameof(BackgroundOperationService)).ForContext("MethodName", nameof(RemoveExpiredOTPAsync)).Information("Remove Expired OTP from table....");

            DateTime expiredTime = DateTime.UtcNow.AddMinutes(0 - _otpSettings.DeleteAfterMinutes);

            var result = await _repositoryContext.UserOtpVerifications.Where(x => x.CreatedAt < expiredTime).ExecuteDeleteAsync();

            Log.ForContext("ClassName", nameof(BackgroundOperationService)).ForContext("MethodName", nameof(RemoveExpiredOTPAsync)).Information("Remove Expired OTP from table returns - {0}", result);

            return result > 0 ?
                GenericResponse<string>.Success("Operation Successful.", $"Expired tokens deleted successfully. Total deleted: {result}", System.Net.HttpStatusCode.OK) :
                GenericResponse<string>.Failure("Operation Failed.", "No token to delete.", System.Net.HttpStatusCode.NotFound);
        }
        catch (Exception ex)
        {
            Log.ForContext("ClassName", nameof(BackgroundOperationService)).ForContext("MethodName", nameof(RemoveExpiredOTPAsync)).Error(ex, "An Error occurred removing expired otp tokens from database");
            return GenericResponse<string>.Failure("Operation Failed.", "An Error Occurred.", System.Net.HttpStatusCode.InternalServerError, new { Message = ex.Message  });
        }
    }

    public async Task<GenericResponse<string>> RemoveExpiredVerificationToken()
    {
        try
        {
            Log.ForContext("ClassName", nameof(BackgroundOperationService)).ForContext("MethodName", nameof(RemoveExpiredVerificationToken)).Information("Remove Expired Verification Tokens.....");

            DateTime expiresAfter = DateTime.UtcNow.AddMinutes(0 - _userOrderVerificationConfig.DeleteAfterInMinutes);

            var result = await _repositoryContext.UserOrderVerificationTokens.Where(x => x.CreatedAt < expiresAfter).ExecuteDeleteAsync();

            Log.ForContext("ClassName", nameof(BackgroundOperationService)).ForContext("MethodName", nameof(RemoveExpiredVerificationToken)).Information("Remove Expired Verification Tokens from database. AFfected records - {0}", result);

            return result > 0 ?
                GenericResponse<string>.Success("Operation Successful.", $"Expired tokens deleted successfully. Total deleted: {result}", System.Net.HttpStatusCode.OK) :
                GenericResponse<string>.Failure("Operation Failed.", "No verification token to delete.", System.Net.HttpStatusCode.NotFound);

        }
        catch (Exception ex)
        {
            Log.ForContext("ClassName", nameof(BackgroundOperationService)).ForContext("MethodName", nameof(RemoveExpiredVerificationToken)).Error(ex, "An Error occurred deleting verification tokens from database.");
            return GenericResponse<string>.Failure("Operation Failed.", "An Error Occurred.", System.Net.HttpStatusCode.InternalServerError, new { Message = ex.Message });
        }
    }
}
