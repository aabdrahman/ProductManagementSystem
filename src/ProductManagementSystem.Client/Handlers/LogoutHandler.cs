using Microsoft.AspNetCore.Components.Authorization;
using ProductManagementSystem.Client.AuthProviderUtility;
using ProductManagementSystem.Client.Utilities.Contracts;

namespace ProductManagementSystem.Client.Handlers;

public class LogoutHandler
{
    private readonly HttpClient _httpClient;
    private readonly ILocalStorageUtility _localStorageUtility;
    private readonly AuthenticationStateProvider _authenticationStateProvider;

    public LogoutHandler(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILocalStorageUtility localStorageUtility, AuthenticationStateProvider authenticationStateProvider)
    {
        _httpClient = httpClientFactory.CreateClient(configuration.GetValue<string>("ApiClient:Key") ?? throw new ArgumentNullException("The api key name is not provided yet"));
        _localStorageUtility = localStorageUtility;
        _authenticationStateProvider = authenticationStateProvider;
    }

    public async Task<bool> Handle()
    {
        try
        {
            var removeFromStorageStatus = await _localStorageUtility.RemoveItemFromStorageAsync("session-token");

            await ((AuthStateProvider)_authenticationStateProvider).NotifyUserLogout();

            _httpClient.DefaultRequestHeaders.Authorization = default;

            return removeFromStorageStatus;

        }
        catch (Exception ex)
        {

            return false;
        }
    }
}
