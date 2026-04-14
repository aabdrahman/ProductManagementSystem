using ProductManagementSystem.Client.Handlers.ClientHelper;
using ProductManagementSystem.Client.Utilities.Contracts;
using ProductManagementSystem.Shared.DataTransferObjects.Order;
using ProductManagementSystem.Shared.DataTransferObjects.RequestParameters;
using ProductManagementSystem.Shared.DataTransferObjects.Response;
using System.Text.Json;

namespace ProductManagementSystem.Client.Handlers;

public class GetParameterizedOrdersHandler : HttpClientProvider
{
    public GetParameterizedOrdersHandler(IHttpClientFactory httpClientFactory, ILocalStorageUtility localStorageUtility, IConfiguration configuration) : base(httpClientFactory, localStorageUtility, configuration)
    {
    }

    public async Task<(PaginatedList<OrderDto>? paginatedOrder, string responseMessage)> Handle(OrderRequestParameters orderRequest)
    {
		try
		{
			HttpClient _httpClient = await GetSecuredHttpClient();
			var httpResponseMessage = await _httpClient.GetAsync($"api/Order/get-orders?StartDate={orderRequest.StartDate}&EndDate={orderRequest.EndDate}&PageNumber={orderRequest.PageNumber}&PageSize={orderRequest.PageSize}&ProductId={orderRequest.ProductId}&OrderStatus={orderRequest.OrderStatus}&TrackingNumber={orderRequest.TrackingNumber}");

			string reponseContent = await httpResponseMessage.Content.ReadAsStringAsync();

			GenericResponse<PaginatedList<OrderDto>> responseBody = JsonSerializer.Deserialize<GenericResponse<PaginatedList<OrderDto>>>(reponseContent, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true })
																		?? throw new ArgumentNullException("Response could not be deserialized.");

			return (responseBody.Data, responseBody.ResponseMessage);
		}
		catch (Exception ex)
		{
			return (null, ex.Message);
		}
    }
}
