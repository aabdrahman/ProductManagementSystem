using ProductManagementSystem.Shared.DataTransferObjects.Order;
using ProductManagementSystem.Shared.DataTransferObjects.Response;
using System.Text.Json;

namespace ProductManagementSystem.Client.Handlers;

public class GetUserOrdersHandler
{
    private readonly HttpClient _httpClient;
    public GetUserOrdersHandler(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _httpClient = httpClientFactory.CreateClient(configuration.GetValue<string>("ApiClient:Key") ?? throw new ArgumentNullException("The api key name is not provided yet"));
    }

    public async Task<(IEnumerable<OrderDto> userOrders, string responseMessage)> Handle(int UserId)
    {
        try
        {
            var httpResponse = await _httpClient.GetAsync($"api/Order/user-orders?UserId={UserId}");

            string httpResponseContent = await httpResponse.Content.ReadAsStringAsync();

            GenericResponse<IEnumerable<OrderDto>> responseBody = JsonSerializer.Deserialize<GenericResponse<IEnumerable<OrderDto>>>(httpResponseContent, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true }) ??
                                                                            throw new ArgumentNullException("Response could not be deserialized");

            return (responseBody.Data ?? [], responseBody.ResponseMessage);
        }
        catch (Exception ex)
        {
            return ([], ex.Message);
        }
    }
}
