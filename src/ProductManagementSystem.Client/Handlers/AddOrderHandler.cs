using ProductManagementSystem.Shared.DataTransferObjects.Order;
using ProductManagementSystem.Shared.DataTransferObjects.Response;
using System.Text.Json;

namespace ProductManagementSystem.Client.Handlers;

public class AddOrderHandler
{
    private readonly HttpClient _httpClient;

    public AddOrderHandler(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _httpClient = httpClientFactory.CreateClient(configuration.GetValue<string>("ApiClient:Key") ?? throw new ArgumentNullException("The api key name is not provided yet"));
    }

    public async Task<(bool isSuccessul, string responseMessage)> Handle(CreateOrderDto createOrder)
    {
        try
        {
            var httpResponse = await _httpClient.PostAsJsonAsync("api/order", createOrder);

            string responseContent = await httpResponse.Content.ReadAsStringAsync();

            GenericResponse<OrderDto> responseBody = JsonSerializer.Deserialize<GenericResponse<OrderDto>>(responseContent, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true }) ??
                                                            throw new ArgumentNullException("Response could not be deserialized.");

            return (responseBody.IsSuccessStatus, responseBody.ResponseMessage);
        }
        catch (Exception ex)
        {

            return (false, ex.Message);
        }
    }
}
