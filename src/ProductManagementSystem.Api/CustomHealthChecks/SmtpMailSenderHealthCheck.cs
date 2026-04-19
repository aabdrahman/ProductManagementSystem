using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using ProductManagementSystem.Api.Entities.ConfigurationModels;
using System.Net.Mail;

namespace ProductManagementSystem.Api.CustomHealthChecks;

public class SmtpMailSenderHealthCheck : IHealthCheck
{
    private readonly IConfiguration _configuration;
    private readonly EmailSettingsConfig _emailSettingsConfig;

    public SmtpMailSenderHealthCheck(IConfiguration configuration, IOptionsMonitor<EmailSettingsConfig> optionsMonitor)
    {
        _configuration = configuration;
        _emailSettingsConfig = optionsMonitor.CurrentValue;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            using var client = new SmtpClient(_emailSettingsConfig.Host, _emailSettingsConfig.Port);
            await Task.Delay(1);
            return HealthCheckResult.Healthy("SMTP available");
        }
        catch (Exception ex)
        {

            return HealthCheckResult.Degraded("SMTP not available", ex);
        }
    }
}
