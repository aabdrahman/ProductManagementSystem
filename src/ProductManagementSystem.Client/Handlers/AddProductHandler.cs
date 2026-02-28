using ProductManagementSystem.Shared.DataTransferObjects.Product;
using ProductManagementSystem.Shared.DataTransferObjects.Response;
using System.Text.Json;

namespace ProductManagementSystem.Client.Handlers;

public class AddProductHandler
{
    private readonly HttpClient _httpClient;

    public AddProductHandler(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _httpClient = httpClientFactory.CreateClient(configuration.GetValue<string>("ApiClient:Key") ?? throw new ArgumentNullException("The api key name is not provided yet"));
    }

    public async Task<(bool isSuccessful, string responseMessage)> Handle(CreateProductDto createProduct)
    {
        try
        {
            var httpResponse = await _httpClient.PostAsJsonAsync("api/product", createProduct);

            string responseContent = await httpResponse.Content.ReadAsStringAsync();

            GenericResponse<ProductDto> responseBody = JsonSerializer.Deserialize<GenericResponse<ProductDto>>(responseContent, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true }) ?? 
                                                            throw new ArgumentNullException("Response could not be deserialized");

            return (responseBody.IsSuccessStatus, responseBody.ResponseMessage);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }
}
