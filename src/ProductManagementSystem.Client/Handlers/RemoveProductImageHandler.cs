using ProductManagementSystem.Client.Handlers.ClientHelper;
using ProductManagementSystem.Client.Utilities.Contracts;

namespace ProductManagementSystem.Client.Handlers;

public class RemoveProductImageHandler : HttpClientProvider
{
    public RemoveProductImageHandler(IHttpClientFactory httpClientFactory, ILocalStorageUtility localStorageUtility, IConfiguration configuration) : base(httpClientFactory, localStorageUtility, configuration)
    {
    }

    public async Task<(bool isSuccessful, string responseMessage)> Handle(List<string> urls)
    {
        try
        {
            int total = urls.Count;
            int successCount = 0; int failureCount = 0;
            List<string> failedItems = [];

            foreach (var item in urls)
            {
                try
                {
                    using (var client = new HttpClient())
                    {
                        var response = await client.DeleteAsync(item);

                        var responseContent = await response.Content.ReadAsStringAsync();

                        if (response.IsSuccessStatusCode)
                        {
                            successCount++;
                        }
                        else
                        {
                            failureCount++;
                            failedItems.Add(item);
                        }
                    }
                }
                catch (Exception ex)
                {

                    failureCount++;
                    failedItems.Add(item);
                }
            }

            return failedItems.Any() ? (false, $"Total removed: {successCount}. Total Faile: {failedItems}") : (true, "Items removed successfully.");
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }
}
