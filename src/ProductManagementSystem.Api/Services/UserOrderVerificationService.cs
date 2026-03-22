using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ProductManagementSystem.Api.Data;
using ProductManagementSystem.Api.Entities.ConfigurationModels;
using ProductManagementSystem.Api.Entities.Models;
using ProductManagementSystem.Api.Services.Contracts;
using Serilog;

namespace ProductManagementSystem.Api.Services;

public class UserOrderVerificationService : IUserOrderVerificationService
{
    private readonly RepositoryContext _repositoryContext;
    private readonly UserOrderVerificationConfig _userOrderVerificationConfig;

    private string _methodName = "MethodName";
    private string _className = "ClassName";

    public UserOrderVerificationService(RepositoryContext repositoryContext, IOptionsMonitor<UserOrderVerificationConfig> optionsMonitor)
    {
        _repositoryContext = repositoryContext;
        _userOrderVerificationConfig = optionsMonitor.CurrentValue;
    }

    public async Task<string> VerifyOrderAsync(string orderVerificationToken)
    {
        try
        {
            Log.ForContext(_methodName, nameof(VerifyOrderAsync)).ForContext(_className, nameof(UserOrderVerificationService)).Information("Verify Order with token - {0}", orderVerificationToken);

            var tokenParams = orderVerificationToken.Split('=');

            string tokenVerificationId = tokenParams[0];
            string token = tokenParams[1];

            if(!Guid.TryParse(tokenVerificationId, out Guid tokenVerificationResultId))
            {
                return "Invalid Token Provided";
            }

            UserOrderVerificationToken? tokenVerificationDetails = await _repositoryContext.UserOrderVerificationTokens.Include(x => x.OrderToConfirm).FirstOrDefaultAsync(x => x.Id == tokenVerificationResultId);


            if(tokenVerificationDetails is null || !tokenVerificationDetails.VerificationToken.Equals(token))
            {
                return "Invalid Token";
            }

            if(tokenVerificationDetails.CreatedAt >= DateTime.UtcNow.AddMinutes(0 - _userOrderVerificationConfig.ExpiresAfterInMinutes))
            {
                return "Token already expired";
            }

            tokenVerificationDetails.OrderToConfirm.IsConfirmed = true;
            tokenVerificationDetails.OrderToConfirm.OrderStatus = Entities.StaticValues.OrderStatus.Confirmed;
            _repositoryContext.UserOrderVerificationTokens.Remove(tokenVerificationDetails);

            await _repositoryContext.SaveChangesAsync();

            Log.ForContext(_methodName, nameof(VerifyOrderAsync)).ForContext(_className, nameof(UserOrderVerificationService)).Information("Order Successfully Confirmed. Tracking Id - {0}", tokenVerificationDetails.OrderToConfirm.OrderTrackingId);

            return "Order Confirmation Successfull.";

        }
        catch (Exception ex)
        {
            Log.ForContext(_methodName, nameof(VerifyOrderAsync)).ForContext(_className, nameof(UserOrderVerificationService)).Error(ex, "An Error Occurred Confirming Order from token");

            return "Order could not be confirmed.";
        }
    }
}
