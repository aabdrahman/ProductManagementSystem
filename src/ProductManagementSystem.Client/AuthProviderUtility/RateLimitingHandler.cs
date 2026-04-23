using System.Net;

namespace ProductManagementSystem.Client.AuthProviderUtility;

public class RateLimitingHandler : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        int retryCount = 0;
        var response = await base.SendAsync(request, cancellationToken);

        while (retryCount <= 3 && response.StatusCode == HttpStatusCode.TooManyRequests)
        {
            await Task.Delay(500);
            response = await base.SendAsync(request, cancellationToken);
            retryCount++;
        }


        return response;
    }
}
