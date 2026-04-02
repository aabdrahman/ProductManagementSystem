namespace ProductManagementSystem.Api.Utilities.Contracts;

public interface IRedisService
{
    Task<bool> SetItemAsync<T>(T data, string key, int timeToExpire = 30);
    Task<T?> GetItemAsync<T>(string key);
    Task<bool> RemoveItemAsync(string key);
    Task<bool> RemoveMultiple(params string[] keys);
}
