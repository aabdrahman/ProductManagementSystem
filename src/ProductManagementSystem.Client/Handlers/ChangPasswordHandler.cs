using ProductManagementSystem.Client.Utilities.Contracts;
using ProductManagementSystem.Shared.DataTransferObjects.Authentication;
using ProductManagementSystem.Shared.DataTransferObjects.Response;
using System.Text.Json;

namespace ProductManagementSystem.Client.Handlers;

public class ChangePasswordHandler
{
    private readonly HttpClient _httpClient;
    private readonly ILocalStorageUtility _localStorageUtility;

    public ChangePasswordHandler(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILocalStorageUtility localStorageUtility)
    {
        _httpClient = httpClientFactory.CreateClient(configuration.GetValue<string>("ApiClient:Key") ?? throw new ArgumentNullException("The api key name is not provided yet"));
        _localStorageUtility = localStorageUtility;
    }

    public async Task<(bool isSuccessful, string responseMessage)> Handle(ChangePasswordDto changePasswordRequest)
    {
        try
        {
            var passwordResetToken = await _localStorageUtility.GetItemFromStorageAsync<string>("password-reset-token");

            _httpClient.DefaultRequestHeaders.Add("X-Password-Reset-Token", passwordResetToken);

            var httpResponse = await _httpClient.PostAsJsonAsync("api/authentication/change-password", changePasswordRequest);

            string httpResponseContent = await httpResponse.Content.ReadAsStringAsync();

            GenericResponse<string> responseBody = JsonSerializer.Deserialize<GenericResponse<string>>(httpResponseContent, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true }) ?? throw new ArgumentNullException("Response could not be deserialized.");

            return (responseBody.IsSuccessStatus, responseBody.ResponseMessage);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }
}
