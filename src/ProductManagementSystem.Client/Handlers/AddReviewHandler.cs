using ProductManagementSystem.Shared.DataTransferObjects.Response;
using ProductManagementSystem.Shared.DataTransferObjects.Review;
using System.Text.Json;

namespace ProductManagementSystem.Client.Handlers;

public class AddReviewHandler
{
    private readonly HttpClient _httpClient;

    public AddReviewHandler(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _httpClient = httpClientFactory.CreateClient(configuration.GetValue<string>("ApiClient:Key") ?? throw new ArgumentNullException("The api key name is not provided yet"));
    }

    public async Task<(bool isSuccessful, string message)> Handle(CreateReviewDto createReview)
    {
        try
        {
            HttpResponseMessage httpResponse = await _httpClient.PostAsJsonAsync("api/review", createReview);

            string responseContent = await httpResponse.Content.ReadAsStringAsync();

            GenericResponse<ReviewDto> responseBody = JsonSerializer.Deserialize<GenericResponse<ReviewDto>>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? throw new JsonException("Failed to deserialize the response body.");

            return (responseBody.IsSuccessStatus, responseBody.ResponseMessage);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }
}
