using ProductManagementSystem.Client.Utilities.Contracts;
using ProductManagementSystem.Shared.DataTransferObjects.Authentication;
using ProductManagementSystem.Shared.DataTransferObjects.Response;
using System.Text.Json;

namespace ProductManagementSystem.Client.Handlers;

public class RefreshTokenHandler
{
    private readonly HttpClient _httpClient;
    private readonly ILocalStorageUtility _localStorageUtility;
    public RefreshTokenHandler(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILocalStorageUtility localStorageUtility)
    {
        _httpClient = httpClientFactory.CreateClient(configuration.GetValue<string>("ApiClient:Key") ?? throw new ArgumentNullException("The api key name is not provided yet"));
        _localStorageUtility = localStorageUtility;
    }

    public async Task<bool> Handle(TokenDto tokenToRefresh)
    {
        try
        {
            var httpResponse = await _httpClient.PostAsJsonAsync("api/Authentication/refresh", tokenToRefresh);

            string responseContent = await httpResponse.Content.ReadAsStringAsync();

            GenericResponse<TokenDto> responseBody = JsonSerializer.Deserialize<GenericResponse<TokenDto>>(responseContent, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true }) ??
                                                            throw new ArgumentNullException("Response could not be deserialized.");


            if (!responseBody.IsSuccessStatus)
            {
                return false;
            }

            var setToStorage = await _localStorageUtility.PersistToStorageAsync<TokenDto>(responseBody.Data, "session-token");

            return setToStorage;
        }
        catch (Exception ex)
        {
            return false;
        }

    }
}