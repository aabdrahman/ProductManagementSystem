using ProductManagementSystem.Shared.DataTransferObjects.ContentDetails;
using ProductManagementSystem.Shared.DataTransferObjects.Response;
using System.Text.Json;

namespace ProductManagementSystem.Client.Handlers;

public class GetAboutUsContentHandler
{
    private readonly HttpClient _httpClient;

    public GetAboutUsContentHandler(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _httpClient = httpClientFactory.CreateClient(configuration.GetValue<string>("ApiClient:Key") ?? throw new ArgumentNullException("The api key name is not provided yet"));
    }

    public async Task<(string responseMessage, IEnumerable<AboutUsDto> contentDetails)> Handle()
    {
        try
        {
            var httpResponse = await _httpClient.GetAsync("api/aboutus");

            string responseContent = await httpResponse.Content.ReadAsStringAsync();

            var responsebody = JsonSerializer.Deserialize<GenericResponse<IEnumerable<AboutUsDto>>>(responseContent, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true })
                                    ?? throw new ArgumentNullException("Respons ecould not be deserialized.");

            return (responsebody.ResponseMessage, responsebody?.Data ?? []);

        }
        catch (Exception ex)
        {
            return (ex.Message, []);
        }
    }
}
