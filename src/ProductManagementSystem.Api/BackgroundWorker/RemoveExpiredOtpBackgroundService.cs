using Microsoft.Extensions.Options;
using ProductManagementSystem.Api.Entities.ConfigurationModels;
using ProductManagementSystem.Api.Services.Contracts;
using Serilog;
using System.Text.Json;

namespace ProductManagementSystem.Api.BackgroundWorker;

public class RemoveExpiredOtpBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly RemoveExpiredOtpBackgroundConfig _removeExpiredOtpBackgroundConfig;

    public RemoveExpiredOtpBackgroundService(IServiceProvider serviceProvider, IOptionsMonitor<RemoveExpiredOtpBackgroundConfig> optionsMonitor)
    {
        _serviceProvider = serviceProvider;
        _removeExpiredOtpBackgroundConfig = optionsMonitor.CurrentValue;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while(!stoppingToken.IsCancellationRequested)
        {
            var timer = new PeriodicTimer(TimeSpan.FromSeconds(_removeExpiredOtpBackgroundConfig.RunAfterSeconds));

            if(await timer.WaitForNextTickAsync())
            {
                Log.ForContext("ClassName", nameof(RemoveExpiredOtpBackgroundService)).ForContext("MethodName", nameof(ExecuteAsync)).Information("Processing Expired OTP deletion operation.....");

                using (var scope = _serviceProvider.CreateScope())
                {
                    var removeOtpService = scope.ServiceProvider.GetRequiredService<IBackgroundOperationService>();

                    var result = await removeOtpService.RemoveExpiredOTPAsync();

                    Log.ForContext("ClassName", nameof(RemoveExpiredOtpBackgroundService)).ForContext("MethodName", nameof(ExecuteAsync)).Information("Expired OTP deletion operation processed successfully - {0}", JsonSerializer.Serialize(result));
                }
            }

            await Task.Delay(TimeSpan.FromSeconds(10));
        }
    }
}
