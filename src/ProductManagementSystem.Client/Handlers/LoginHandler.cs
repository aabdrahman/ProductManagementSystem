using ProductManagementSystem.Client.Utilities.Contracts;
using ProductManagementSystem.Shared.DataTransferObjects.Authentication;
using ProductManagementSystem.Shared.DataTransferObjects.Response;
using System.Text.Json;

namespace ProductManagementSystem.Client.Handlers;

public class LoginHandler
{
    private readonly HttpClient _httpClient;
    private readonly ILocalStorageUtility _localStorageUtility;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<LoginHandler> _logger;

    public LoginHandler(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILocalStorageUtility localStorageUtility, IHttpContextAccessor httpContextAccessor, ILogger<LoginHandler> logger)
    {
        _httpClient = httpClientFactory.CreateClient(configuration.GetValue<string>("ApiClient:Key") ?? throw new ArgumentNullException("The api key name is not provided yet"));
        _localStorageUtility = localStorageUtility;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task<(bool isSuccessful, string message)> Handle(LoginUserDto loginUser)
    {
        try
        {
            _logger.LogInformation("Calling login handler with - {0}", loginUser);
            HttpResponseMessage httpResponse = await _httpClient.PostAsJsonAsync("api/Authentication/login", loginUser);
            _logger.LogInformation("HTTP returns - {0}", httpResponse.StatusCode);

            string httpResponseContent = await httpResponse.Content.ReadAsStringAsync();

            _logger.LogInformation($"Response content: {httpResponseContent}");

            GenericResponse<TokenDto> responseBody = JsonSerializer.Deserialize<GenericResponse<TokenDto>>(httpResponseContent, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true }) ??
                                                                throw new ArgumentNullException("Response could not be deserialized.");

            if (responseBody.IsSuccessStatus)
            {
                bool isStorageMaintained = await _localStorageUtility.PersistToStorageAsync<TokenDto>(responseBody.Data, "session-token");

                return isStorageMaintained ? (responseBody.IsSuccessStatus, $"{responseBody.ResponseMessage}.{"Session Activated"}") : (isStorageMaintained, $"{responseBody.ResponseMessage}{"Session activation failed."}");
            }

            return (responseBody.IsSuccessStatus, responseBody.ResponseMessage);

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);// ("Error occurred calling the login endpoint: {0}. Message: {1}. Stack Trace: {2}", ex.GetType().ToString(), ex.Message, ex.StackTrace);
            return (false, ex.Message);
        }
    }
}
