using Microsoft.AspNetCore.Components;

namespace ProductManagementSystem.Client.AuthProviderUtility;

public class OriginHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public OriginHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        request.Headers.Remove("Origin");

        var httpRequest = _httpContextAccessor.HttpContext.Request;
        var baseUrl = $"{httpRequest.Scheme}://{httpRequest.Host}";

        request.Headers.Add("Origin", baseUrl);

        return await base.SendAsync(request, cancellationToken);
    }
}
