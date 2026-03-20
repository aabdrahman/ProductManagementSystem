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
    private readonly RemoveExpiredOtpBackgroundConfig _removeExpiredOtpBackgroundConfig;
    private readonly OtpSettingsConfig _otpSettings;

    public BackgroundOperationService(RepositoryContext repositoryContext, IOptionsMonitor<RemoveExpiredOtpBackgroundConfig> optionsMonitor, IOptionsMonitor<OtpSettingsConfig> otpSettingsOptionsMonitor)
    {
        _repositoryContext = repositoryContext;
        _removeExpiredOtpBackgroundConfig = optionsMonitor.CurrentValue;
        _otpSettings = otpSettingsOptionsMonitor.CurrentValue;
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
}
