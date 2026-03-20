using Microsoft.Extensions.Options;
using ProductManagementSystem.Api.Entities.ConfigurationModels;
using ProductManagementSystem.Api.Utilities.Contracts;
using Serilog;

namespace ProductManagementSystem.Api.BackgroundWorker;

public class EmailProcessingBackgroundService : BackgroundService
{
    private readonly EmailSettingsWorkerConfig _emailSettingsWorkerConfig;
    private readonly IServiceProvider _serviceProvider;

    public EmailProcessingBackgroundService(IOptionsMonitor<EmailSettingsWorkerConfig> optionsMonitor, IServiceProvider serviceProvider)
    {
        _emailSettingsWorkerConfig = optionsMonitor.CurrentValue;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {

        while (!stoppingToken.IsCancellationRequested)
        {
            var timer = new PeriodicTimer(TimeSpan.FromSeconds(_emailSettingsWorkerConfig.RunAfterSeconds));

            if(await timer.WaitForNextTickAsync())
            {
                Log.ForContext("ClassName", nameof(EmailProcessingBackgroundService)).ForContext("MethodName", nameof(ExecuteAsync)).Information("Processing Queued Emails........");

                using (var scope = _serviceProvider.CreateScope())
                {
                    var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

                    var result = await emailService.ProcessQueuedEmails();

                    Log.ForContext("ClassName", nameof(EmailProcessingBackgroundService)).ForContext("MethodName", nameof(ExecuteAsync)).Information("Queued Emails Processed - {0}", result);
                }
            }

            await Task.Delay(1000);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        Log.ForContext("ClassName", nameof(EmailProcessingBackgroundService)).ForContext("MethodName", nameof(StopAsync)).Information("Email Sending Worker is stopping.....");

        using (var scope = _serviceProvider.CreateScope())
        {
            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

            var result = await emailService.ProcessQueuedEmails(true);

            Log.ForContext("ClassName", nameof(EmailProcessingBackgroundService)).ForContext("MethodName", nameof(ExecuteAsync)).Information("Queued Emails Processed before stopping worker - {0}", result);
        }
    }
}
