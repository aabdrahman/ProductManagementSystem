using Microsoft.AspNetCore.Components;

namespace ProductManagementSystem.Client.AuthProviderUtility;

public class OriginHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly NavigationManager _navigationManager;
    private readonly IConfiguration _configuration;

    public OriginHandler(IHttpContextAccessor httpContextAccessor, NavigationManager navigationManager, IConfiguration configuration)
    {
        _httpContextAccessor = httpContextAccessor;
        _navigationManager = navigationManager;
        _configuration = configuration;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        request.Headers.Remove("Origin");

        var httpRequest = _httpContextAccessor.HttpContext?.Request;

        var baseUrl = httpRequest is not null ? $"{httpRequest.Scheme}://{httpRequest.Host}" : _configuration.GetValue<string>("Client-Url")?.TrimEnd('/');

        request.Headers.Add("Origin", baseUrl);

        return await base.SendAsync(request, cancellationToken);
    }
}
