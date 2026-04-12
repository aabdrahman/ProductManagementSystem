using ProductManagementSystem.Client.Handlers.ClientHelper;
using ProductManagementSystem.Client.Utilities.Contracts;
using ProductManagementSystem.Shared.DataTransferObjects.Order;
using ProductManagementSystem.Shared.DataTransferObjects.Response;
using System.Text.Json;

namespace ProductManagementSystem.Client.Handlers;

public class GetOrdersHandler : HttpClientProvider
{
    //private readonly HttpClient _httpClient;

    //public GetOrdersHandler(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    //{
    //    _httpClient = httpClientFactory.CreateClient(configuration.GetValue<string>("ApiClient:Key") ?? throw new ArgumentNullException("The api key name is not provided yet"));
    //}

    public GetOrdersHandler(IHttpClientFactory httpClientFactory, ILocalStorageUtility localStorageUtility, IConfiguration configuration) : base(httpClientFactory, localStorageUtility, configuration)
    {
    }

    public async Task<(IEnumerable<OrderDto> orders, string responseMessage)> Handle()
    {
        try
        {
            var _httpClient = await GetSecuredHttpClient();
            var httpResponse = await _httpClient.GetAsync("api/order");

            string responseContent = await httpResponse.Content.ReadAsStringAsync();

            GenericResponse<IEnumerable<OrderDto>> responseBody = JsonSerializer.Deserialize<GenericResponse<IEnumerable<OrderDto>>>(responseContent, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true }) ??
                                                                        throw new ArgumentNullException("Respons ecould not be deserialized.");

            return (responseBody.Data ?? [], responseBody.ResponseMessage);
        }
        catch (Exception ex)
        {
            return ([], ex.Message);
        }
    }
}
