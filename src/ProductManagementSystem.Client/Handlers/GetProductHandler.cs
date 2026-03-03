using ProductManagementSystem.Shared.DataTransferObjects.Product;
using ProductManagementSystem.Shared.DataTransferObjects.Response;
using System.Text.Json;

namespace ProductManagementSystem.Client.Handlers;

public class GetProductHandler
{
    private readonly HttpClient _httpClient;

    public GetProductHandler(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _httpClient = httpClientFactory.CreateClient(configuration.GetValue<string>("ApiClient:Key") ?? throw new ArgumentNullException("Api Key cannot be found."));
    }

    public async Task<(ProductDto? product, string responseMessage)> Handle(int Id)
    {
        try
        {
            var httpResponse = await _httpClient.GetAsync($"api/product/{Id}");

            string responseContent = await httpResponse.Content.ReadAsStringAsync();

            GenericResponse<ProductDto> responseBody = JsonSerializer.Deserialize<GenericResponse<ProductDto>>(responseContent, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true }) ?? throw new ArgumentNullException("Response could not be deserialized");

            return (responseBody.Data, responseBody.ResponseMessage);
        }
        catch (Exception ex)
        {
            return (null, ex.Message);
        }
    }
}
