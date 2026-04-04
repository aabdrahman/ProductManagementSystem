namespace ProductManagementSystem.Client.Utilities.Contracts;

public interface ILocalStorageUtility
{
    Task<bool> PersistToStorageAsync<T>(T item, string key);
    Task<T?> GetItemFromStorageAsync<T>(string key);
    Task<bool> RemoveItemFromStorageAsync(string key);
    Task<bool> RemoveAllItemsFromStorageAsync(params string[] keys);
}
