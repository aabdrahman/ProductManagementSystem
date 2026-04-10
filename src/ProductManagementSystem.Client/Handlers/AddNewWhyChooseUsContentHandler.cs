using ProductManagementSystem.Client.Handlers.ClientHelper;
using ProductManagementSystem.Client.Utilities.Contracts;
using ProductManagementSystem.Shared.DataTransferObjects.ContentDetails;
using ProductManagementSystem.Shared.DataTransferObjects.Response;
using System.Text.Json;

namespace ProductManagementSystem.Client.Handlers;

public class AddNewWhyChooseUsContentHandler : HttpClientProvider
{
    public AddNewWhyChooseUsContentHandler(IHttpClientFactory httpClientFactory, ILocalStorageUtility localStorageUtility, IConfiguration configuration) : base(httpClientFactory, localStorageUtility, configuration)
    {
    }

    //private readonly HttpClient _httpClient;

    //public AddNewWhyChooseUsContentHandler(IConfiguration configuration, IHttpClientFactory httpClientFactory)
    //{
    //    _httpClient = httpClientFactory.CreateClient(configuration.GetValue<string>("ApiClient:Secure-Key") ?? throw new ArgumentNullException("The api key name is not provided yet"));
    //}

    public async Task<(bool isSuccessful, string responseMessage)> Handle(CreateWhyChooseUsDto createWhyChooseUs)
    {
        try
        {
            var _httpClient = await base.GetSecuredHttpClient();
            var httpResponse = await _httpClient.PostAsJsonAsync("api/WhyChooseUs", createWhyChooseUs);

            string responseContent = await httpResponse.Content.ReadAsStringAsync();

            GenericResponse<string> responseBody = JsonSerializer.Deserialize<GenericResponse<string>>(responseContent, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true }) ??
                                                                    throw new ArgumentNullException("Respons ecould not be deserialized.");

            return (responseBody.IsSuccessStatus, responseBody.ResponseMessage);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }
}
