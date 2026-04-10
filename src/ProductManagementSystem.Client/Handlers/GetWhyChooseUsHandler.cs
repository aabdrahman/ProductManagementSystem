using ProductManagementSystem.Shared.DataTransferObjects.ContentDetails;
using ProductManagementSystem.Shared.DataTransferObjects.Response;
using System.Text.Json;

namespace ProductManagementSystem.Client.Handlers;

public class GetWhyChooseUsHandler
{
    private readonly HttpClient _httpClient;

    public GetWhyChooseUsHandler(IConfiguration configuration, IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient(configuration.GetValue<string>("ApiClient:Secure-Key") ?? throw new ArgumentNullException("The api key name is not provided yet"));
    }

    public async Task<(string responseMessage, IEnumerable<WhyChooseUsDto> contentItems)> Handle()
    {
        try
        {

            var httpResponse = await _httpClient.GetAsync("api/WhyChooseUs");

            string responseContent = await httpResponse.Content.ReadAsStringAsync();

            GenericResponse<IEnumerable<WhyChooseUsDto>> responseBody = JsonSerializer.Deserialize<GenericResponse<IEnumerable<WhyChooseUsDto>>>(responseContent, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true }) ??
                                                                                throw new ArgumentNullException("Response could not be deserialized.");

            return (responseBody.ResponseMessage, responseBody.Data ?? []);
        }
        catch (Exception ex)
        {
            return (ex.Message, []);
        }
    }
}
