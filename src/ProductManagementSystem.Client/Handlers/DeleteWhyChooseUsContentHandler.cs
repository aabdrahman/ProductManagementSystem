using ProductManagementSystem.Client.Handlers.ClientHelper;
using ProductManagementSystem.Client.Utilities.Contracts;
using ProductManagementSystem.Shared.DataTransferObjects.Response;
using System.Text.Json;

namespace ProductManagementSystem.Client.Handlers;

public class DeleteWhyChooseUsContentHandler : HttpClientProvider
{
    public DeleteWhyChooseUsContentHandler(IHttpClientFactory httpClientFactory, ILocalStorageUtility localStorageUtility, IConfiguration configuration) : base(httpClientFactory, localStorageUtility, configuration)
    {
    }

    public async Task<(bool isSuccessful, string responseMessage)> Handle(Guid Id)
    {
		try
		{
			var _httpClient = await GetSecuredHttpClient();
			var httpResponse = await _httpClient.DeleteAsync($"api/whychooseus/{Id}");

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
