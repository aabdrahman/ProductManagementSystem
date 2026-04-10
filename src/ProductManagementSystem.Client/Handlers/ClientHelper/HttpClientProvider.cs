using ProductManagementSystem.Client.Utilities.Contracts;
using ProductManagementSystem.Shared.DataTransferObjects.Authentication;

namespace ProductManagementSystem.Client.Handlers.ClientHelper;

public abstract class HttpClientProvider
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILocalStorageUtility _localStorageUtility;
    private readonly IConfiguration _configuration;

    protected HttpClientProvider(IHttpClientFactory httpClientFactory, ILocalStorageUtility localStorageUtility, IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _localStorageUtility = localStorageUtility;
        _configuration = configuration;
    }

    public async Task<HttpClient> GetSecuredHttpClient()
    {

        try
        {
            var tokenDetails = await _localStorageUtility.GetItemFromStorageAsync<TokenDto>("session-token");
            var _httpClient = _httpClientFactory.CreateClient(_configuration.GetValue<string>("ApiClient:Key") ?? throw new ArgumentNullException("The api key name is not provided yet"));


            if(_httpClient is not null)
            {
                _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenDetails?.Token ?? "");
            }

            return _httpClient;
        }
        catch (Exception ex)
        {
            return default;
        }
        


    }
}
