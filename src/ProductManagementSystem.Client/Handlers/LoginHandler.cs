using ProductManagementSystem.Client.Utilities.Contracts;
using ProductManagementSystem.Shared.DataTransferObjects.Authentication;
using ProductManagementSystem.Shared.DataTransferObjects.Response;
using System.Text.Json;

namespace ProductManagementSystem.Client.Handlers;

public class LoginHandler
{
    private readonly HttpClient _httpClient;
    private readonly ILocalStorageUtility _localStorageUtility;

    public LoginHandler(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILocalStorageUtility localStorageUtility)
    {
        _httpClient = httpClientFactory.CreateClient(configuration.GetValue<string>("ApiClient:Key") ?? throw new ArgumentNullException("The api key name is not provided yet"));
        _localStorageUtility = localStorageUtility;
    }

    public async Task<(bool isSuccessful, string message)> Handle(LoginUserDto loginUser)
    {
        try
        {
            HttpResponseMessage httpResponse = await _httpClient.PostAsJsonAsync("api/Authentication/login", loginUser);

            string httpResponseContent = await httpResponse.Content.ReadAsStringAsync();

            GenericResponse<TokenDto> responseBody = JsonSerializer.Deserialize<GenericResponse<TokenDto>>(httpResponseContent, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true }) ??
                                                                throw new ArgumentNullException("Response could not be deserialized.");

            if (responseBody.IsSuccessStatus)
            {
                bool isStorageMaintained = await _localStorageUtility.PersistToStorageAsync<TokenDto>(responseBody.Data, "session-token");

                return isStorageMaintained ? (responseBody.IsSuccessStatus, $"{responseBody.ResponseMessage}.{"Session Activated"}") : (isStorageMaintained, $"{responseBody.ResponseMessage}{"Session activation failed."}");
            }

            return (responseBody.IsSuccessStatus, responseBody.ResponseMessage);

        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }
}
