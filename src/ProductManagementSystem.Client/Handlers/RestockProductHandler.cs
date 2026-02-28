using ProductManagementSystem.Shared.DataTransferObjects.Product;
using ProductManagementSystem.Shared.DataTransferObjects.Response;
using System.Text.Json;

namespace ProductManagementSystem.Client.Handlers;

public class RestockProductHandler
{
    private readonly HttpClient _httpClient;

    public RestockProductHandler(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _httpClient = httpClientFactory.CreateClient(configuration.GetValue<string>("ApiClient:Key") ?? throw new ArgumentNullException("The api key name is not provided yet"));
    }

    public async Task<(bool isSuccessful, string responseMessage)> Handle(UpdateProductStockDto updateProductStock)
    {
        try
        {
            HttpResponseMessage httpResponse = await _httpClient.PatchAsJsonAsync("api/Product/update-stock", updateProductStock);

            string responseContent = await httpResponse.Content.ReadAsStringAsync();

            GenericResponse<string> responseBody = JsonSerializer.Deserialize<GenericResponse<string>>(responseContent, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true }) ?? 
                                    throw new ArgumentNullException("Response could not be deserialized");

            return (responseBody.IsSuccessStatus, responseBody.ResponseMessage);

        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }
}
