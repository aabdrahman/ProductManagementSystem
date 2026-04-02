using ProductManagementSystem.Api.Entities.ChannelBrokers;
using ProductManagementSystem.Api.Utilities.Contracts;
using Serilog;
using System.Threading.Channels;

namespace ProductManagementSystem.Api.BackgroundWorker;

public class PriorityEmailProcessorBackgroundService : BackgroundService
{
    private readonly Channel<SendPriorityMailEvent> _channel;
    private readonly IServiceScopeFactory _scopeFactory;

    public PriorityEmailProcessorBackgroundService(Channel<SendPriorityMailEvent> channel, IServiceScopeFactory scopeFactory)
    {
        _channel = channel;
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(10000);
        while (!stoppingToken.IsCancellationRequested)
        {
            Log.ForContext("ClassName", nameof(PriorityEmailProcessorBackgroundService)).ForContext("MethodName", nameof(ExecuteAsync)).Information("Running high priority mail processor....");
            try
            {
                using (var scope = _scopeFactory.CreateScope())
                {
                    var emailProcessor = scope.ServiceProvider.GetRequiredService<IEmailService>();

                    var result = await emailProcessor.ProcessPriorityMails();

                    Log.ForContext("ClassName", nameof(PriorityEmailProcessorBackgroundService)).ForContext("MethodName", nameof(ExecuteAsync)).Information("Processed high priority mails result - {0}", result);
                }
            }
            catch (Exception ex)
            {
                Log.ForContext("ClassName", nameof(PriorityEmailProcessorBackgroundService)).ForContext("MethodName", nameof(ExecuteAsync)).Error(ex, "High priority mails could not be processed");
            }

            await Task.Delay(5000);
        }
    }


    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _channel.Writer.Complete();
        return base.StopAsync(cancellationToken);
    }
}
