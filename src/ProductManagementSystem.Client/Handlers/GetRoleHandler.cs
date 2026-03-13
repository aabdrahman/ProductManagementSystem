using ProductManagementSystem.Shared.DataTransferObjects.Response;
using ProductManagementSystem.Shared.DataTransferObjects.Role;
using System.Text.Json;

namespace ProductManagementSystem.Client.Handlers;

public class GetRoleHandler
{
    private readonly HttpClient _httpClient;

    public GetRoleHandler(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _httpClient = httpClientFactory.CreateClient(configuration.GetValue<string>("ApiClient:Key") ?? throw new ArgumentNullException("The api key name is not provided yet"));
    }

    public async Task<(IEnumerable<RoleDto> roles, string message)> Handle()
    {
        try
        {
            var httpResponse = await _httpClient.GetAsync("api/role");

            string httpResponseContent = await httpResponse.Content.ReadAsStringAsync();

            GenericResponse<IEnumerable<RoleDto>> responseBody = JsonSerializer.Deserialize<GenericResponse<IEnumerable<RoleDto>>>(httpResponseContent, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true }) ??
                                                                                    throw new ArgumentNullException("Response could not ne deserailized.");

            return (responseBody.Data, responseBody.ResponseMessage);
        }
        catch (Exception ex)
        {
            return ([], ex.Message);
        }
    }
}
