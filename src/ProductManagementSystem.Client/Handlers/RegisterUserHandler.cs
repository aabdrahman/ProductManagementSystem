using ProductManagementSystem.Shared.DataTransferObjects.Response;
using ProductManagementSystem.Shared.DataTransferObjects.User;
using System.Text.Json;

namespace ProductManagementSystem.Client.Handlers;

public class RegisterUserHandler
{
    private readonly HttpClient _httpClient;

    public RegisterUserHandler(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _httpClient = httpClientFactory.CreateClient(configuration.GetValue<string>("ApiClient:Key") ?? throw new ArgumentNullException("The api key name is not provided yet"));
    }

    public async Task<(bool isSuccessful, string responseMessage)> Handle(CreateUserDto createUser)
    {
        try
        {
            var httpResponse = await _httpClient.PostAsJsonAsync("api/user", createUser);

            string responseContent = await httpResponse.Content.ReadAsStringAsync();

            GenericResponse<UserDto> responseBody = JsonSerializer.Deserialize<GenericResponse<UserDto>>(responseContent, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true }) ??
                                                            throw new ArgumentNullException("Response could not be deserialized.");

            return (responseBody.IsSuccessStatus, responseBody.ResponseMessage);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }
}
