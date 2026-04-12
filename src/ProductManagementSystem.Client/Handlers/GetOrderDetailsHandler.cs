using ProductManagementSystem.Client.Handlers.ClientHelper;
using ProductManagementSystem.Client.Utilities.Contracts;
using ProductManagementSystem.Shared.DataTransferObjects.Order;
using ProductManagementSystem.Shared.DataTransferObjects.Response;
using System.Text.Json;

namespace ProductManagementSystem.Client.Handlers;

public class GetOrderDetailsHandler : HttpClientProvider
{
    //private readonly HttpClient _httpClient;

    //public GetOrderDetailsHandler(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    //{
    //    _httpClient = httpClientFactory.CreateClient(configuration.GetValue<string>("ApiClient:Key") ?? throw new ArgumentNullException("The api key name is not provided yet"));
    //}

    public GetOrderDetailsHandler(IHttpClientFactory httpClientFactory, ILocalStorageUtility localStorageUtility, IConfiguration configuration) : base(httpClientFactory, localStorageUtility, configuration)
    {
    }

    public async Task<(OrderDetailsDto? orderDetails, string responseMessage)> Handle(int OrderId)
    {
        try
        {
            var _httpClient = await GetSecuredHttpClient();
            var httpResponse = await _httpClient.GetAsync($"api/order/details/{OrderId}");

            string responseContent = await httpResponse.Content.ReadAsStringAsync();

            GenericResponse<OrderDetailsDto> responseBody = JsonSerializer.Deserialize<GenericResponse<OrderDetailsDto>>(responseContent, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true }) ??
                                                                    throw new ArgumentNullException("Response could not be deserialized.");

            return (responseBody.Data, responseBody.ResponseMessage);
        }
        catch (Exception ex)
        {
            return (null, ex.Message);
        }
    }

}
