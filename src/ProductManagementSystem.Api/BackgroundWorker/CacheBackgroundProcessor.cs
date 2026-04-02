using ProductManagementSystem.Api.Entities.ChannelBrokers;
using ProductManagementSystem.Api.Utilities;
using ProductManagementSystem.Api.Utilities.Contracts;
using Serilog;
using System.Reflection;
using System.Text.Json;
using System.Threading.Channels;

namespace ProductManagementSystem.Api.BackgroundWorker;

public class CacheBackgroundProcessor : BackgroundService
{
    private readonly Channel<CacheItemProcessor<CacheItem>> _channel;
    private readonly Assembly _assembly;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public CacheBackgroundProcessor(Channel<CacheItemProcessor<CacheItem>> channel, IServiceScopeFactory serviceScopeFactory)
    {
        _channel = channel;
        _serviceScopeFactory = serviceScopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using (var scope = _serviceScopeFactory.CreateScope())
            {

                var redisCacheProcessor = scope.ServiceProvider.GetRequiredService<IRedisService>();

                var assemblyItems = AppDomain.CurrentDomain.GetAssemblies();

                await foreach (var cacheItemProcessor in _channel.Reader.ReadAllAsync(stoppingToken))
                {
                    try
                    {
                        Log.ForContext("ClassName", nameof(CacheBackgroundProcessor)).ForContext("MethodName", nameof(ExecuteAsync)).Information("Processing cache item of type {CacheItemType}. Value - {cacheItemValue}", cacheItemProcessor.Data.Type, cacheItemProcessor.Data.Value);

                        var cacheItemType = assemblyItems.Select(x => x.GetType(cacheItemProcessor.Data.Type)).FirstOrDefault(x => x is not null);

                        //Type? cacheItemType = Assembly..GetType(cacheItemProcessor.Data.Type);

                        var objectToCache = JsonSerializer.Deserialize(cacheItemProcessor.Data.Value, cacheItemType, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

                        var result = await redisCacheProcessor.SetItemAsync(objectToCache, cacheItemProcessor.Data.Key, (int)cacheItemProcessor.Data.ExpiresAfter);

                        Log.ForContext("ClassName", nameof(CacheBackgroundProcessor)).ForContext("MethodName", nameof(ExecuteAsync)).Information("Cache item of type {CacheItemType} with key {CacheItemKey} has been cached successfully. Result - {cacheResult}", cacheItemProcessor.Data.Type, cacheItemProcessor.Data.Key, result);
                    }
                    catch (Exception ex)
                    {

                        Log.ForContext("ClassName", nameof(CacheBackgroundProcessor)).ForContext("MethodName", nameof(ExecuteAsync)).Error(ex, "An error occurred while processing cache item of type {CacheItemType} with key {CacheItemKey}", cacheItemProcessor.Data.Type, cacheItemProcessor.Data.Key);
                    }
                }
            }

            await Task.Delay(TimeSpan.FromSeconds(1));
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _channel.Writer.Complete();

        await ProcessAllItems();
    }


    private async Task ProcessAllItems()
    {
        Log.ForContext("ClassName", nameof(CacheBackgroundProcessor)).ForContext("MethodName", nameof(ProcessAllItems)).Information("Processing remaining cache items in the channel before stopping the background processor.");

        using (var scope = _serviceScopeFactory.CreateScope())
        {
            var redisCacheProcessor = scope.ServiceProvider.GetRequiredService<IRedisService>();

            var assemblyItems = AppDomain.CurrentDomain.GetAssemblies();

            await foreach (var cacheItemProcessor in _channel.Reader.ReadAllAsync(new CancellationToken()))
            {
                try
                {
                    Log.ForContext("ClassName", nameof(CacheBackgroundProcessor)).ForContext("MethodName", nameof(ProcessAllItems)).Information("Processing cache item of type {CacheItemType}. Value - {cacheItemValue}", cacheItemProcessor.Data.Type, cacheItemProcessor.Data.Value);
                    
                    var cacheItemType = assemblyItems.Select(x => x.GetType(cacheItemProcessor.Data.Type)).FirstOrDefault(x => x is not null);

                    //Type? cacheItemType = Assembly..GetType(cacheItemProcessor.Data.Type);

                    var objectToCache = JsonSerializer.Deserialize(cacheItemProcessor.Data.Value, cacheItemType, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

                    var result = await redisCacheProcessor.SetItemAsync(objectToCache, cacheItemProcessor.Data.Key, (int)cacheItemProcessor.Data.ExpiresAfter);

                    Log.ForContext("ClassName", nameof(CacheBackgroundProcessor)).ForContext("MethodName", nameof(ExecuteAsync)).Information("Cache item of type {CacheItemType} with key {CacheItemKey} has been cached successfully. Result - {cacheResult}", cacheItemProcessor.Data.Type, cacheItemProcessor.Data.Key, result);
                }
                catch (Exception ex)
                {

                    Log.ForContext("ClassName", nameof(CacheBackgroundProcessor)).ForContext("MethodName", nameof(ExecuteAsync)).Error(ex, "An error occurred while processing cache item of type {CacheItemType} with key {CacheItemKey}", cacheItemProcessor.Data.Type, cacheItemProcessor.Data.Key);
                }
            }
        }

        Log.ForContext("ClassName", nameof(CacheBackgroundProcessor)).ForContext("MethodName", nameof(ProcessAllItems)).Information("Finished processing remaining cache items in the channel.");
    }
}
