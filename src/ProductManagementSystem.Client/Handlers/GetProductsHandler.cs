using ProductManagementSystem.Shared.DataTransferObjects.Product;
using ProductManagementSystem.Shared.DataTransferObjects.Response;
using System.Text.Json;

namespace ProductManagementSystem.Client.Handlers;

public class GetProductsHandler
{
    private readonly HttpClient _httpClient;

    public GetProductsHandler(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _httpClient = httpClientFactory.CreateClient(configuration.GetValue<string>("ApiClient:Key") ?? throw new ArgumentNullException("The api key name is not provided yet"));
    }

    public async Task<(IEnumerable<ProductDto> products, string message)> Handle()
    {
        try
        {
            var httpResponse = await _httpClient.GetAsync("api/product");

            string responseContent = await httpResponse.Content.ReadAsStringAsync();

            GenericResponse<IEnumerable<ProductDto>> responseBody = JsonSerializer.Deserialize<GenericResponse<IEnumerable<ProductDto>>>(responseContent, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true }) ?? throw new ArgumentException("Response could not be deserialized.");

            return (responseBody.Data ?? [], responseBody.ResponseMessage);

        }
        catch (Exception ex)
        {

            return([], ex.Message);
        }
    }
}
