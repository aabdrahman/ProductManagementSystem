using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ProductManagementSystem.Api.Data;
using ProductManagementSystem.Api.Entities.ConfigurationModels;
using ProductManagementSystem.Api.Helpers;
using ProductManagementSystem.Api.Services.Contracts;
using ProductManagementSystem.Api.Utilities.Contracts;
using ProductManagementSystem.Shared.DataTransferObjects.Order;
using ProductManagementSystem.Shared.DataTransferObjects.OrderLineItem;
using ProductManagementSystem.Shared.DataTransferObjects.Response;
using Serilog;
using System.Text;

namespace ProductManagementSystem.Api.Services;

public class BackgroundOperationService : IBackgroundOperationService
{
    private readonly RepositoryContext _repositoryContext;
    private readonly IEmailService _emailService;
    private readonly OtpSettingsConfig _otpSettings;
    private readonly UserOrderVerificationConfig _userOrderVerificationConfig;
    private List<int> SuccessfulProcessedIds = [];

    public BackgroundOperationService(RepositoryContext repositoryContext, IOptionsMonitor<RemoveExpiredOtpBackgroundConfig> optionsMonitor,
                                        IOptionsMonitor<OtpSettingsConfig> otpSettingsOptionsMonitor, IOptionsMonitor<UserOrderVerificationConfig> userOrderVerificationOptionsMonitor, 
                                        IEmailService emailService)
    {
        _repositoryContext = repositoryContext;
        _otpSettings = otpSettingsOptionsMonitor.CurrentValue;
        _userOrderVerificationConfig = userOrderVerificationOptionsMonitor.CurrentValue;
        _emailService = emailService;
    }

    public async Task<GenericResponse<string>> ProcessOrderConfirmationNotification()
    {
        try
        {
            Log.ForContext("ClassName", nameof(BackgroundOperationService)).ForContext("MethodName", nameof(ProcessOrderConfirmationNotification)).Information("Processing Sending Confirmed Order Notifications...");

            List<OrderDetailsDto> ordersToProcessNotification = await _repositoryContext.Orders.AsNoTracking().Where(x => !x.IsConfirmationNotificationSent && x.IsConfirmed)
                                                            .Select(x => new OrderDetailsDto()
                                                            {
                                                                Id = x.Id,
                                                                CreatedBy = x.CreatedByUser.UserEmailAddress,
                                                                CreatedDate = x.CreatedAt.ToLocalTime(),
                                                                DeliveryAddress = x.DeliveryAddress,
                                                                OrderStatus = x.OrderStatus.ToString(),
                                                                OrderNumber = x.OrderTrackingId,
                                                                IsConfirmed = x.IsConfirmed,
                                                                CreatedByUser = string.Concat(x.CreatedByUser.FirstName, " ", x.CreatedByUser.LastName),
                                                                OrderLineItems = x.OrderLineItems.Select(oli => new OrderLineItemDetailsDto()
                                                                {
                                                                    Id = oli.Id,
                                                                    IsActive = oli.IsActive,
                                                                    ProductName = oli.OrderedProduct.NormalizedName,
                                                                    OrderCount = oli.QuantityOrdered,
                                                                    Price = oli.OrderRate * oli.QuantityOrdered
                                                                }).ToList()
                                                            }).Take(5)
                                                            .ToListAsync();

            Log.ForContext("ClassName", nameof(BackgroundOperationService)).ForContext("MethodName", nameof(ProcessOrderConfirmationNotification)).Information("Total Notification to send - {0}", ordersToProcessNotification.Count);

            if(ordersToProcessNotification.Count == 0)
            {
                return GenericResponse<string>.Success("No record exist to process", "No record exists to process.", System.Net.HttpStatusCode.OK);
            }

            string orderLineItemElement = @"
                                                <tr>
                                                    <td style=""padding: 15px 0; vertical-align: middle;"">
                                                        <span style=""font-weight: 600; color: #24292e;"">{{ProductName}}</span>
                                                    </td>
                                                    <td style=""padding: 15px 0; text-align: center; color: #586069;"">{{ProductCount}}</td>
                                                    <td style=""padding: 15px 0; text-align: right; font-weight: 700; color: #1a1a1a;"">{{Price}}</td>
                                                </tr>
                                        ";
            int totalToProcess = ordersToProcessNotification.Count;
            int totalSuccess = 0;
            int totalFailed = 0;

            foreach (var orderDetail in ordersToProcessNotification)
            {
                var emailContentParameter = new Dictionary<string, string>();
                emailContentParameter.Add("OrderNumber", orderDetail.OrderNumber);
                emailContentParameter.Add("UserName", orderDetail.CreatedByUser);
                emailContentParameter.Add("OrderDate", orderDetail.CreatedDate.ToString("dd MMM YYYY"));
                emailContentParameter.Add("FullAddress", orderDetail.DeliveryAddress.Replace("\n", "<br/>").Replace("\r\n", "<br/>").Trim());
                emailContentParameter.Add("Year", DateTime.Now.Year.ToString());
                emailContentParameter.Add("Fee", (0.00).ToString("C"));
                emailContentParameter.Add("TotalAmount", orderDetail.OrderLineItems.Select(x => x.Price).Sum().ToString("C"));

                StringBuilder orderLineItemsFullElement = new StringBuilder();

                foreach (var orderLineItem in orderDetail.OrderLineItems)
                {
                    string orderLineParsedElement = orderLineItemElement.Replace("{{ProductName}}", orderLineItem.ProductName)
                                                                        .Replace("{{Price}}", orderLineItem.Price.ToString("C"))
                                                                        .Replace("{{ProductCount}}", orderLineItem.OrderCount.ToString());

                    orderLineItemsFullElement.AppendLine(orderLineParsedElement);
                }

                emailContentParameter.Add("OrderLineItems", orderLineItemsFullElement.ToString());

                try
                {
                    var sendEmailResult = await _emailService.SendEmailAsync(new Shared.DataTransferObjects.MailOperation.EmailSenderDto(Subject: $"Order Confirmation Notification - {orderDetail.OrderNumber}", Content: EmailContentHelper.GetMailContent("ConfirmedOrderNotification.html", emailContentParameter), Recipients: [orderDetail.CreatedBy.ToLower()], isHtml: true));

                    Log.ForContext("ClassName", nameof(BackgroundOperationService)).ForContext("MethodName", nameof(ProcessOrderConfirmationNotification)).Information("Send Order Confirmation Notification Result - {0} for Order - {1}", sendEmailResult, orderDetail.OrderNumber);

                    if (sendEmailResult)
                    {
                        SuccessfulProcessedIds.Add(orderDetail.Id);
                        totalSuccess++;
                    }
                    else
                    {
                        totalFailed++;
                    }
                }
                catch (Exception ex)
                {
                    Log.ForContext("ClassName", nameof(BackgroundOperationService)).ForContext("MethodName", nameof(ProcessOrderConfirmationNotification)).Error(ex, "An Error Occurred Processing Sending Order Confirmation Notification");
                }
            }

            return GenericResponse<string>.Success("Background prcoess run successfully.", $"Total Processed:{totalToProcess}, Total Successful:{totalSuccess},, Total Failed:{totalFailed}", System.Net.HttpStatusCode.OK);

        }
        catch (Exception ex)
        {
            Log.ForContext("ClassName", nameof(BackgroundOperationService)).ForContext("MethodName", nameof(ProcessOrderConfirmationNotification)).Error(ex, "An Error Occurred Processing Notification.");
            return GenericResponse<string>.Failure("Notification Send Failed.", "An error Occurred sending order confirmation notification.", System.Net.HttpStatusCode.OK);
        }
        finally
        {
            if (SuccessfulProcessedIds.Any())
            {
                var updateState = await _repositoryContext.Orders.Where(x => SuccessfulProcessedIds.Contains(x.Id)).ExecuteUpdateAsync(x => x.SetProperty(x => x.IsConfirmationNotificationSent, true));

                Log.ForContext("ClassName", nameof(BackgroundOperationService)).ForContext("MethodName", nameof(ProcessOrderConfirmationNotification)).Information("Setting Order Confirmation for successfult Ids - {0}. Returns - {1}", SuccessfulProcessedIds, updateState);
            }

            
        }
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
