using ProductManagementSystem.Shared.DataTransferObjects.ProductCategory;
using ProductManagementSystem.Shared.DataTransferObjects.Response;
using System.Text.Json;

namespace ProductManagementSystem.Client.Handlers;

public class GetProductCategoryHandler
{
    private readonly HttpClient _httpClient;

    public GetProductCategoryHandler(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _httpClient = httpClientFactory.CreateClient(configuration.GetValue<string>("ApiClient:Key") ?? throw new ArgumentNullException("The api key name is not provided yet."));
    }

    public async Task<(IEnumerable<ProductCategoryDto> productCategories, string message)> Handle()
    {
        try
        {
            var httpResponse = await _httpClient.GetAsync("api/productcategory");

            string responseContent = await httpResponse.Content.ReadAsStringAsync();

            GenericResponse<IEnumerable<ProductCategoryDto>> responseBody = JsonSerializer.Deserialize<GenericResponse<IEnumerable<ProductCategoryDto>>>(responseContent, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true  }) ?? throw new ArgumentNullException("Failed to deserialize the response body.");

            return (responseBody.Data ?? [], responseBody.ResponseMessage);
        }
        catch (Exception ex)
        {
            return ([], ex.Message);
        }
    }
}
