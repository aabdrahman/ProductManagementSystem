using ProductManagementSystem.Api.Services.Contracts;
using Serilog;
using System.Text.Json;

namespace ProductManagementSystem.Api.BackgroundWorker;

public class SendOrderConfirmationNotificationBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    public SendOrderConfirmationNotificationBackgroundService(IServiceScopeFactory serviceScopeFactory)
    {
        _serviceScopeFactory = serviceScopeFactory;
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var timer = new PeriodicTimer(TimeSpan.FromMinutes(15));

            if(await timer.WaitForNextTickAsync(stoppingToken))
            {

                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    Log.ForContext("ClassName", nameof(SendOrderConfirmationNotificationBackgroundService)).ForContext("MethodName", nameof(ExecuteAsync)).Information("Processing Confirmed Orders Notification...");

                    var backgroundOperationService = scope.ServiceProvider.GetRequiredService<IBackgroundOperationService>();

                    var response = await backgroundOperationService.ProcessOrderConfirmationNotification();

                    Log.ForContext("ClassName", nameof(SendOrderConfirmationNotificationBackgroundService)).ForContext("MethodName", nameof(ExecuteAsync)).Information("Confirmed Order Notification Processed Successfully - {0}", JsonSerializer.Serialize(response));
                }
            }

            await Task.Delay(TimeSpan.FromSeconds(10));
        }
    }
}
