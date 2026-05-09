using Microsoft.AspNetCore.Components.Forms;
using ProductManagementSystem.Client.Handlers.ClientHelper;
using ProductManagementSystem.Client.Utilities.Contracts;

namespace ProductManagementSystem.Client.Handlers;

public class AddProductImageHandler : HttpClientProvider
{
    public AddProductImageHandler(IHttpClientFactory httpClientFactory, ILocalStorageUtility localStorageUtility, IConfiguration configuration) : base(httpClientFactory, localStorageUtility, configuration)
    {
    }

    public async Task<(bool isSuccessful, string responseMessage)> Handle(int productId, List<IBrowserFile> productImages)
    {
        try
        {
            var _httpClient = await GetSecuredHttpClient();
            using var content = new MultipartFormDataContent();

            foreach (var image in productImages)
            {
                var streamContent = new StreamContent(image.OpenReadStream());

                content.Add(streamContent, "productImages", image.Name);
            }
            var httpResponse = await _httpClient.PostAsync($"api/ProductImage/{productId}", content);

            string responseContent = await httpResponse.Content.ReadAsStringAsync();

            return (httpResponse.IsSuccessStatusCode, responseContent);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }
}
