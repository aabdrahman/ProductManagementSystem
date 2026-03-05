using ProductManagementSystem.Shared.DataTransferObjects.Order;
using ProductManagementSystem.Shared.DataTransferObjects.Response;
using System.Text.Json;

namespace ProductManagementSystem.Client.Handlers;

public class UpdateOrderStatusHandler
{
    private readonly HttpClient _httpClient;
    public UpdateOrderStatusHandler(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _httpClient = httpClientFactory.CreateClient(configuration.GetValue<string>("ApiClient:Key") ?? throw new ArgumentNullException("The api key name is not provided yet"));
    }

    public async Task<(bool isSuccessful, string responseMessage)> Handle(UpdateOrderStatusDto updateOrderStatus)
    {
        try
        {
            var httpResponse = await _httpClient.PutAsJsonAsync("api/order/update-status", updateOrderStatus);

            string responseContent = await httpResponse.Content.ReadAsStringAsync();

            GenericResponse<string> responseBody = JsonSerializer.Deserialize<GenericResponse<string>>(responseContent, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true }) ??
                                                                throw new ArgumentNullException("Respons ecould not be deserialized");

            return (responseBody.IsSuccessStatus, responseBody.ResponseMessage);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }
}
