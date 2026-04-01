using ProductManagementSystem.Api.Utilities.Contracts;
using Serilog;
using StackExchange.Redis;
using System.Text.Json;

namespace ProductManagementSystem.Api.Utilities;

public class RedisService : IRedisService
{
    private readonly IDatabase _redisDatabase;

    public RedisService(IConnectionMultiplexer connectionMultiplexer)
    {
        _redisDatabase = connectionMultiplexer.GetDatabase();
    }
    public async Task<T?> GetItemAsync<T>(string key)
    {
        try
        {
            Log.ForContext("ClassName", nameof(RedisService)).ForContext("MethodName", nameof(GetItemAsync)).Information("Retrieving item from Redis cache with key: {Key}.", key);

            var result = await _redisDatabase.StringGetAsync(key);

            return result.IsNullOrEmpty ? default(T?) : JsonSerializer.Deserialize<T>(result, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

        }
        catch (Exception ex)
        {
            Log.ForContext("ClassName", nameof(RedisService)).ForContext("MethodName", nameof(GetItemAsync)).Error(ex, "An error occurred while retrieving item from Redis cache.");
            return default(T?);
        }

    }

    public async Task<bool> RemoveItemAsync(string key)
    {
        try
        {
            Log.ForContext("ClassName", nameof(RedisService)).ForContext("MethodName", nameof(RemoveItemAsync)).Information("Removing item from Redis cache with key: {Key}.", key);

            var result = await _redisDatabase.StringDeleteAsync(key, When.Always);

            return result;

        }
        catch (Exception ex)
        {
            Log.ForContext("ClassName", nameof(RedisService)).ForContext("MethodName", nameof(RemoveItemAsync)).Error(ex, "An error occurred while removing item from Redis cache.");

            return false;
        }
    }

    public async Task<bool> SetItemAsync<T>(T data, string key, int timeToExpire = 30)
    {
        try
        {
            Log.ForContext("ClassName", nameof(RedisService)).ForContext("MethodName", nameof(SetItemAsync)).Information("Setting item in Redis cache with key: {Key} and expiration time: {ExpirationTime} seconds.", key, timeToExpire);

            var jsonData = JsonSerializer.Serialize(data);

            var result = await _redisDatabase.StringSetAsync(key, jsonData, TimeSpan.FromSeconds(timeToExpire));

            return result;
        }
        catch (Exception ex)
        {
            Log.ForContext("ClassName", nameof(RedisService)).ForContext("MethodName", nameof(SetItemAsync)).Error(ex, "An error occurred while setting item in Redis cache.");
            return false;
        }

        throw new NotImplementedException();
    }
}
