using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using ProductManagementSystem.Client.Utilities.Contracts;

namespace ProductManagementSystem.Client.Utilities;

public class LocalStorageUtility : ILocalStorageUtility
{
    private readonly ProtectedLocalStorage _protectedLocalStorage;

    public LocalStorageUtility(ProtectedLocalStorage protectedLocalStorage)
    {
        _protectedLocalStorage = protectedLocalStorage;
    }

    public async Task<T?> GetItemFromStorageAsync<T>(string key)
    {
        try
        {
            var result = await _protectedLocalStorage.GetAsync<T>(key.ToUpper());

            return result.Value;
        }
        catch (Exception ex)
        {
            return default;
        }
    }

    public async Task<bool> PersistToStorageAsync<T>(T item, string key)
    {
        try
        {
            if(item is null)
            {
                return false;
            }

            await _protectedLocalStorage.SetAsync(key.ToUpper(), item);

            return true;
        }
        catch (Exception ex)
        {
            return false;
        }
    }

    public async Task<bool> RemoveAllItemsFromStorageAsync(params string[] keys)
    {
        try
        {
            foreach (var item in keys)
            {
                await _protectedLocalStorage.DeleteAsync(item.ToUpper());
            }

            return true;
        }
        catch (Exception ex)
        {
            return false;
        }

        throw new NotImplementedException();
    }

    public async Task<bool> RemoveItemFromStorageAsync(string key)
    {
        try
        {
            await _protectedLocalStorage.DeleteAsync(key.ToUpper());

            return true;
        }
        catch (Exception ex)
        {
            return false;
        }
    }
}
