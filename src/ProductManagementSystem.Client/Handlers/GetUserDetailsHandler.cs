using ProductManagementSystem.Client.Handlers.ClientHelper;
using ProductManagementSystem.Client.Utilities.Contracts;
using ProductManagementSystem.Shared.DataTransferObjects.Response;
using ProductManagementSystem.Shared.DataTransferObjects.User;
using System.Text.Json;

namespace ProductManagementSystem.Client.Handlers;

public class GetUserDetailsHandler : HttpClientProvider
{
    public GetUserDetailsHandler(IHttpClientFactory httpClientFactory, ILocalStorageUtility localStorageUtility, IConfiguration configuration) : base(httpClientFactory, localStorageUtility, configuration)
    {
    }

    //private readonly HttpClient _httpClient;

    //public GetUserDetailsHandler(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    //{
    //    _httpClient = httpClientFactory.CreateClient(configuration.GetValue<string>("ApiClient:Key") ?? throw new ArgumentNullException("The api key name is not provided yet"));
    //}

    public async Task<(bool isSuccessful, UserDto? userDetails)> Handle(int Id)
    {
        try
        {
            HttpClient _httpClient = await GetSecuredHttpClient();
            HttpResponseMessage httpResponse = await _httpClient.GetAsync($"api/User/{Id}");

            string httpResponseContent = await httpResponse.Content.ReadAsStringAsync();

            GenericResponse<UserDto> responseBody = JsonSerializer.Deserialize<GenericResponse<UserDto>>(httpResponseContent, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true }) ??
                                                            throw new ArgumentNullException("Response could not be deserialized.");

            return (responseBody.IsSuccessStatus, responseBody.Data);
        }
        catch (Exception ex)
        {
            return (false, null);
        }
    }
 }
