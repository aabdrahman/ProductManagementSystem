using Microsoft.AspNetCore.Authentication;
using ProductManagementSystem.Client.Handlers;
using ProductManagementSystem.Client.Utilities;
using ProductManagementSystem.Client.Utilities.Contracts;
using ProductManagementSystem.Shared.DataTransferObjects.Authentication;

namespace ProductManagementSystem.Client.AuthProviderUtility;

public class AuthProviderHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _contextAccessor;
    private readonly ILocalStorageUtility _localStorageUtility;
    private readonly RefreshTokenHandler _refreshTokenHandler;
    private readonly TokenContainer _tokenContainer;

    public AuthProviderHandler(IHttpContextAccessor contextAccessor, ILocalStorageUtility localStorageUtility, RefreshTokenHandler refreshTokenHandler, TokenContainer tokenContainer)
    {
        _contextAccessor = contextAccessor;
        _localStorageUtility = localStorageUtility;
        _refreshTokenHandler = refreshTokenHandler;
        _tokenContainer = tokenContainer;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var httpRequest = _contextAccessor.HttpContext.Request;
        var baseUrl = $"{httpRequest.Scheme}://{httpRequest.Host}";

        request.Headers.Add("Origin", baseUrl);

        var tokenDetails = await _localStorageUtility.GetItemFromStorageAsync<TokenDto>("session-token");
        var token = await _contextAccessor.HttpContext.GetTokenAsync("access_token");

        token = _tokenContainer.GetToken();


        if(tokenDetails is not null || !string.IsNullOrEmpty(token))
        {
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenDetails?.Token ?? token);

            var response = await base.SendAsync(request, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return response;
            }

            if(response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                try
                {
                    var refreshTokenStatus = await _refreshTokenHandler.Handle(tokenDetails);

                    if (refreshTokenStatus)
                    {
                        tokenDetails = await _localStorageUtility.GetItemFromStorageAsync<TokenDto>("session-token");

                        if(tokenDetails is not null)
                        {
                            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenDetails.Token);

                            response = await base.SendAsync(request, cancellationToken);

                            return response;
                        }
                    }
                    else
                    {
                        return response;
                    }

                }
                catch (HttpRequestException ex)
                {

                    return response;
                }
                
            }
        }


        return await base.SendAsync(request, cancellationToken);

    }
}
