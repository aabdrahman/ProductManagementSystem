using Microsoft.AspNetCore.Components.Authorization;
using ProductManagementSystem.Client.AuthProviderUtility;
using ProductManagementSystem.Client.Utilities.Contracts;
using ProductManagementSystem.Shared.DataTransferObjects.Authentication;
using ProductManagementSystem.Shared.DataTransferObjects.Response;
using System.Text.Json;

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

    public async Task<(bool isSuccessful, string responseMessage)> Handle()
    {
        try
        {
            var tokenDetails = await _localStorageUtility.GetItemFromStorageAsync<TokenDto>("session-token");

            if(tokenDetails is not null)
            {
                var httpResponse = await _httpClient.PostAsJsonAsync("api/Authentication/logout", tokenDetails);

                string httpResponseContent = await httpResponse.Content.ReadAsStringAsync();

                GenericResponse<string>? responseBody = JsonSerializer.Deserialize<GenericResponse<string>>(httpResponseContent, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true }) ?? null;

                var removeFromStorageStatus2 = await _localStorageUtility.RemoveItemFromStorageAsync("session-token");

                await ((AuthStateProvider)_authenticationStateProvider).NotifyUserLogout();

                _httpClient.DefaultRequestHeaders.Authorization = default;

                return (removeFromStorageStatus2, responseBody?.ResponseMessage ?? "User session logged out successfully.");

            }

            var removeFromStorageStatus = await _localStorageUtility.RemoveItemFromStorageAsync("session-token");

            await ((AuthStateProvider)_authenticationStateProvider).NotifyUserLogout();

            _httpClient.DefaultRequestHeaders.Authorization = default;

            return (removeFromStorageStatus, "User session logged out successfully.");

        }
        catch (Exception ex)
        {

            return (false, ex.Message);
        }
    }
}
