using ProductManagementSystem.Shared.DataTransferObjects.ProductCategory;
using ProductManagementSystem.Shared.DataTransferObjects.Response;
using System.Text.Json;

namespace ProductManagementSystem.Client.Handlers;

public class AddProductCategoryHandler
{
    private readonly HttpClient _httpClient;

    public AddProductCategoryHandler(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _httpClient = httpClientFactory.CreateClient(configuration.GetValue<string>("ApiClient:Key") ?? throw new ArgumentNullException("The api key name is not provided yet"));
    }

    public async Task<(bool isSuccessful, string responseMessage)> Handle(string productCategoryToCreate)
    {
        try
        {
            var httpResponse = await _httpClient.PostAsJsonAsync("api/productcategory", productCategoryToCreate);

            string responseContent = await httpResponse.Content.ReadAsStringAsync();

            GenericResponse<ProductCategoryDto> responseBody = JsonSerializer.Deserialize<GenericResponse<ProductCategoryDto>>(responseContent, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true }) ??
                                                                    throw new ArgumentNullException("Response could not be deserialized.");

            return (responseBody.IsSuccessStatus, responseBody.ResponseMessage);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }
}
