using Microsoft.Extensions.Options;
using ProductManagementSystem.Api.Entities.ConfigurationModels;
using ProductManagementSystem.Api.Services.Contracts;
using Serilog;
using System.Text.Json;

namespace ProductManagementSystem.Api.BackgroundWorker;

public class RemoveExpiredVerificationTokenBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly RemoveExpiredVerificationTokenBackgroundConfig _removeExpiredVerificationTokenBackgroundConfig;

    public RemoveExpiredVerificationTokenBackgroundService(IServiceProvider serviceProvider, IOptionsMonitor<RemoveExpiredVerificationTokenBackgroundConfig> optionsMonitor)
    {
        _serviceProvider = serviceProvider;
        _removeExpiredVerificationTokenBackgroundConfig = optionsMonitor.CurrentValue;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while(!stoppingToken.IsCancellationRequested)
        {
            var timer = new PeriodicTimer(TimeSpan.FromSeconds(_removeExpiredVerificationTokenBackgroundConfig.RunAfterSeconds));

            if (await timer.WaitForNextTickAsync())
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var removeVerificationTokenService = scope.ServiceProvider.GetRequiredService<IBackgroundOperationService>();

                    var result = await removeVerificationTokenService.RemoveExpiredVerificationToken();

                    Log.ForContext("ClassName", nameof(RemoveExpiredVerificationTokenBackgroundService)).ForContext("MethodName", nameof(ExecuteAsync)).Information("Expired Verification Token operation processed successfully - {0}", JsonSerializer.Serialize(result));
                }
            }

            await Task.Delay(TimeSpan.FromSeconds(10));
        }
    }
}