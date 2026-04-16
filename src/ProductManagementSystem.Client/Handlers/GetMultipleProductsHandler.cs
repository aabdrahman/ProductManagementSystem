using ProductManagementSystem.Client.Handlers.ClientHelper;
using ProductManagementSystem.Client.Utilities.Contracts;
using ProductManagementSystem.Shared.DataTransferObjects.Product;
using ProductManagementSystem.Shared.DataTransferObjects.Response;
using System.Text.Json;

namespace ProductManagementSystem.Client.Handlers;

public class GetMultipleProductsHandler : HttpClientProvider
{
    public GetMultipleProductsHandler(IHttpClientFactory httpClientFactory, ILocalStorageUtility localStorageUtility, IConfiguration configuration) : base(httpClientFactory, localStorageUtility, configuration)
    {
    }

    //private readonly HttpClient _httpClient;

    //public GetMultipleProductsHandler(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    //{
    //    _httpClient = httpClientFactory.CreateClient(configuration.GetValue<string>("ApiClient:Key") ?? throw new ArgumentNullException("The api key name is not provided yet."));
    //}

    public async Task<(IEnumerable<ProductDto> products, string message)> Handle(IEnumerable<int> Ids)
    {
        try
        {
            HttpClient _httpClient = await GetSecuredHttpClient();
            string queryString = "";

            foreach (var id in Ids)
            {
                queryString = $"{queryString}&Ids={id}";
            }

            var httpResponse = await _httpClient.GetAsync($"api/product/collection?{queryString}");

            string responseContent = await httpResponse.Content.ReadAsStringAsync();

            GenericResponse<IEnumerable<ProductDto>> responseBody = JsonSerializer.Deserialize<GenericResponse<IEnumerable<ProductDto>>>(responseContent, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true })
                                ?? throw new ArgumentNullException("Response could not be deseralized.");

            return (responseBody.Data, responseBody.ResponseMessage);
        }
        catch (Exception ex)
        {
            return ([], ex.Message);
        }
    }
}
