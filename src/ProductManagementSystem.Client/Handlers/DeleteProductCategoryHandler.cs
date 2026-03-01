using ProductManagementSystem.Shared.DataTransferObjects.Response;
using System.Text.Json;

namespace ProductManagementSystem.Client.Handlers;

public class DeleteProductCategoryHandler
{
    private readonly HttpClient _httpClient;

    public DeleteProductCategoryHandler(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _httpClient = httpClientFactory.CreateClient(configuration.GetValue<string>("ApiClient:Key") ?? throw new ArgumentNullException("The api key name is not provided yet"));
    }

    public async Task<(bool isSuccessful, string responseMessage)> Handle(int categoryId)
    {
        try
        {
            var httpResponse = await _httpClient.DeleteAsync($"api/productcategory/{categoryId}");

            string reponseContent = await httpResponse.Content.ReadAsStringAsync();

            GenericResponse<string> responseBody = JsonSerializer.Deserialize<GenericResponse<string>>(reponseContent, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true }) ??
                                                                    throw new ArgumentNullException("Response could not be deserialized.");

            return (responseBody.IsSuccessStatus, responseBody.ResponseMessage);
        }
        catch (Exception ex)
        {

            return (false, ex.Message);
        }
    }
}
