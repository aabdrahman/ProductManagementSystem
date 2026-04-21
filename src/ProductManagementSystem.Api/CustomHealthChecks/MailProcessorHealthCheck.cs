using Microsoft.Extensions.Diagnostics.HealthChecks;
using ProductManagementSystem.Api.Utilities.Contracts;

namespace ProductManagementSystem.Api.CustomHealthChecks;

public class MailProcessorHealthCheck : IHealthCheck
{
    private readonly IEmailService _emailService;

    public MailProcessorHealthCheck(IEmailService emailService)
    {
        _emailService = emailService;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _emailService.GetPriorityMails();

            var data = new Dictionary<string, object>()
            {
                ["Total Queued Count"] = result.queuedCount,
                ["Total Temp Queued Count"] = result.tempQueuedCount
            };

            if(result.queuedCount > 100)
            {
                return HealthCheckResult.Degraded("The queued count has exceeded 100 mails.", data: data);
            }

            if(result.tempQueuedCount > 50)
            {
                return HealthCheckResult.Degraded("The temporary queued count has exceeded 50 mails. Somthing might be wrong", data: data);
            }


            return HealthCheckResult.Healthy("Email processor is running optimally.", data: data);
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Degraded("An error occurred while getting the total count of queued email.", ex);
        }
    }
}
