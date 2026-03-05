using ProductManagementSystem.Shared.DataTransferObjects.Response;
using ProductManagementSystem.Shared.DataTransferObjects.Review;
using System.Text.Json;

namespace ProductManagementSystem.Client.Handlers;

public class GetReviewsHandler
{
    private readonly HttpClient _httpClient;

    public GetReviewsHandler(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _httpClient = httpClientFactory.CreateClient(configuration.GetValue<string>("ApiClient:Key") ?? throw new ArgumentNullException("The api key name is not provided yet"));
    }

    public async Task<(IEnumerable<ReviewDto> reviews, string message)> Handle()
    {
        try
        {
            HttpResponseMessage httpResponse = await _httpClient.GetAsync("api/review");

            string httpResponseContent = await httpResponse.Content.ReadAsStringAsync();

            GenericResponse<IEnumerable<ReviewDto>> responseBody = JsonSerializer.Deserialize<GenericResponse<IEnumerable<ReviewDto>>>(httpResponseContent, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true }) ?? 
                                                                        throw new ArgumentException("Response could not be deserialized");

            return (responseBody.Data, responseBody.ResponseMessage);
        }
        catch (Exception ex)
        {
            return ([], ex.Message);
        }
    }
}
