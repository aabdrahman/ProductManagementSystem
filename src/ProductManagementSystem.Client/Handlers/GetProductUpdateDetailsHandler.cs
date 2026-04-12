using ProductManagementSystem.Client.Handlers.ClientHelper;
using ProductManagementSystem.Client.Utilities.Contracts;
using ProductManagementSystem.Shared.DataTransferObjects.Product;
using ProductManagementSystem.Shared.DataTransferObjects.Response;
using System.Text.Json;

namespace ProductManagementSystem.Client.Handlers;

public class GetProductUpdateDetailsHandler : HttpClientProvider
{
    //private readonly HttpClient _httpClient;

    //public GetProductUpdateDetailsHandler(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    //{
    //    _httpClient = httpClientFactory.CreateClient(configuration.GetValue<string>("ApiClient:Key") ?? throw new ArgumentNullException("The api key name is not provided yet"));
    //}

    public GetProductUpdateDetailsHandler(IHttpClientFactory httpClientFactory, ILocalStorageUtility localStorageUtility, IConfiguration configuration) : base(httpClientFactory, localStorageUtility, configuration)
    {
    }

    public async Task<(UpdateProductDto? productToUpdate, string responseMessage)> Handle(int Id)
    {
        try
        {
            HttpClient _httpClient = await GetSecuredHttpClient();
            var httpResponse = await _httpClient.GetAsync($"api/Product/get-product-update-details/{Id}");

            string responseContent = await httpResponse.Content.ReadAsStringAsync();

            GenericResponse<UpdateProductDto> responseBody = JsonSerializer.Deserialize<GenericResponse<UpdateProductDto>>(responseContent, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true }) ??
                                                                throw new ArgumentNullException("Response could not be deserialized.");

            return (responseBody.Data, responseBody.ResponseMessage);
        }
        catch (Exception ex)
        {

            return (null, ex.Message);
        }
    }
}
