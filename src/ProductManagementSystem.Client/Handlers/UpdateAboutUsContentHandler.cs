using ProductManagementSystem.Client.Handlers.ClientHelper;
using ProductManagementSystem.Client.Utilities.Contracts;
using ProductManagementSystem.Shared.DataTransferObjects.ContentDetails;
using ProductManagementSystem.Shared.DataTransferObjects.Response;
using System.Text.Json;

namespace ProductManagementSystem.Client.Handlers;

public class UpdateAboutUsContentHandler : HttpClientProvider
{
    public UpdateAboutUsContentHandler(IHttpClientFactory httpClientFactory, ILocalStorageUtility localStorageUtility, IConfiguration configuration) : base(httpClientFactory, localStorageUtility, configuration)
    {
    }

    public async Task<(bool isSuccesful, string responseMessage)> Handle(UpdateAboutUsDto updateAboutUs)
    {
        try
        {
            var _httpClient = await GetSecuredHttpClient();
            var httpResponse = await _httpClient.PutAsJsonAsync("api/aboutus", updateAboutUs);

            string responseContent = await httpResponse.Content.ReadAsStringAsync();

            var responseBody = JsonSerializer.Deserialize<GenericResponse<string>>(responseContent, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true })
                                                ?? throw new ArgumentNullException("Respons eould not be deserialized.");

            return (responseBody.IsSuccessStatus, responseBody.ResponseMessage);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }
}
