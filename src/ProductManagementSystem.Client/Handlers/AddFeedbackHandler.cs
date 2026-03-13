using ProductManagementSystem.Shared.DataTransferObjects.Feedback;
using ProductManagementSystem.Shared.DataTransferObjects.Response;
using System.Text.Json;

namespace ProductManagementSystem.Client.Handlers;

public class AddFeedbackHandler
{
    private readonly HttpClient _httpClient;

    public AddFeedbackHandler(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _httpClient = httpClientFactory.CreateClient(configuration.GetValue<string>("ApiClient:Key") ?? throw new ArgumentNullException("The api key name is not provided yet"));
    }

    public async Task<(bool isSuccessful, string message)> Handle(CreateFeedbackDto createFeedback)
    {
        try
        {
            var httpResponse = await _httpClient.PostAsJsonAsync("api/feedback", createFeedback);

            string responseContent = await httpResponse.Content.ReadAsStringAsync();

            GenericResponse<FeedbackDto> responseBody = JsonSerializer.Deserialize<GenericResponse<FeedbackDto>>(responseContent, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true }) ??
                                                                throw new ArgumentNullException("Respons ecould not be deserialized.");

            return (responseBody.IsSuccessStatus, responseBody.ResponseMessage);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }
}
