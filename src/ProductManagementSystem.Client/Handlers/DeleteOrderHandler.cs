using ProductManagementSystem.Client.Handlers.ClientHelper;
using ProductManagementSystem.Client.Utilities.Contracts;
using ProductManagementSystem.Shared.DataTransferObjects.Response;
using System.Text.Json;

namespace ProductManagementSystem.Client.Handlers;

public class DeleteOrderHandler : HttpClientProvider
{
    //private readonly HttpClient _httpClient;

    //public DeleteOrderHandler(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    //{
    //    _httpClient = httpClientFactory.CreateClient(configuration.GetValue<string>("ApiClient:Key") ?? throw new ArgumentNullException("The api key name is not provided yet"));
    //}

    public DeleteOrderHandler(IHttpClientFactory httpClientFactory, ILocalStorageUtility localStorageUtility, IConfiguration configuration) : base(httpClientFactory, localStorageUtility, configuration)
    {
    }

    public async Task<(bool isSuccessful, string responseMessage)> Handle(int Id)
    {
        try
        {
            HttpClient _httpClient = await GetSecuredHttpClient();
            var httpResponse = await _httpClient.DeleteAsync($"api/order/{Id}");

            string responseContent = await httpResponse.Content.ReadAsStringAsync();

            GenericResponse<string> responseBody = JsonSerializer.Deserialize<GenericResponse<string>>(responseContent, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true }) ??
                                                        throw new ArgumentNullException("Respons ecould not be deserialized.");

            return (responseBody.IsSuccessStatus, responseBody.ResponseMessage);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }
}
