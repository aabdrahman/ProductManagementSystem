using Microsoft.AspNetCore.Components.Authorization;
using ProductManagementSystem.Client.Handlers;
using ProductManagementSystem.Client.Utilities;
using ProductManagementSystem.Client.Utilities.Contracts;
using ProductManagementSystem.Shared.DataTransferObjects.Authentication;
using System.Security.Claims;

namespace ProductManagementSystem.Client.AuthProviderUtility;

public class AuthStateProvider : AuthenticationStateProvider
{
    private readonly ILocalStorageUtility _storageUtility;
    private readonly RefreshTokenHandler _refreshTokenHandler;
    private AuthenticationState _anonymous;
    private readonly TokenContainer _tokenContainer;

    public AuthStateProvider(ILocalStorageUtility storageUtility, TokenContainer tokenContainer, RefreshTokenHandler refreshTokenHandler)
    {
        _storageUtility = storageUtility;
        _anonymous = new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        _tokenContainer = tokenContainer;
        _refreshTokenHandler = refreshTokenHandler;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        TokenDto? storedToken = await _storageUtility.GetItemFromStorageAsync<TokenDto>("session-token");

        if(storedToken is null)
        {
            NotifyAuthenticationStateChanged(Task.FromResult(_anonymous));
            return _anonymous;
        }

        var claimsPrincipal = JwtParser.ParseClaimsFromJwt(storedToken.Token);

        if(DateTime.UtcNow >= storedToken.TokenExpirationTime)
        {
            var removeTokenResult = await _storageUtility.RemoveItemFromStorageAsync("session-token");
            NotifyAuthenticationStateChanged(Task.FromResult(_anonymous));
            return _anonymous;
        }

        string expTokenTime = claimsPrincipal.FirstOrDefault(x => x.Type.Contains("exp", StringComparison.CurrentCultureIgnoreCase))?.Value ?? "";

        if(!long.TryParse(expTokenTime, out long expiryTime))
        {
            var removeTokenResult = await _storageUtility.RemoveItemFromStorageAsync("session-token");
            NotifyAuthenticationStateChanged(Task.FromResult(_anonymous));
            return _anonymous;
        }

        DateTimeOffset expiryTimestamp = DateTimeOffset.FromUnixTimeSeconds(expiryTime);

        if(DateTimeOffset.UtcNow > expiryTimestamp || (DateTimeOffset.UtcNow - expiryTimestamp).TotalSeconds <= 5)
        {
            //Begin Refresh Token implementation
            if(DateTime.UtcNow > storedToken.TokenExpirationTime || (DateTimeOffset.UtcNow - expiryTimestamp).TotalSeconds <= 5)
            {
                var refreshTokenResult = await _refreshTokenHandler.Handle(storedToken);

                if (refreshTokenResult)
                {
                    storedToken = await _storageUtility.GetItemFromStorageAsync<TokenDto>("session-token");

                    if (storedToken is null)
                    {
                        NotifyAuthenticationStateChanged(Task.FromResult(_anonymous));
                        return _anonymous;
                    }

                    claimsPrincipal = JwtParser.ParseClaimsFromJwt(storedToken.Token);

                    if (DateTime.UtcNow >= storedToken.TokenExpirationTime)
                    {
                        var removeTokenResult = await _storageUtility.RemoveItemFromStorageAsync("session-token");
                        NotifyAuthenticationStateChanged(Task.FromResult(_anonymous));
                        return _anonymous;
                    }

                    expTokenTime = claimsPrincipal.FirstOrDefault(x => x.Type.Contains("exp", StringComparison.CurrentCultureIgnoreCase))?.Value ?? "";

                    if (!long.TryParse(expTokenTime, out expiryTime))
                    {
                        var removeTokenResult = await _storageUtility.RemoveItemFromStorageAsync("session-token");
                        NotifyAuthenticationStateChanged(Task.FromResult(_anonymous));
                        return _anonymous;
                    }
                }
                else
                {
                    var removeTokenResult = await _storageUtility.RemoveItemFromStorageAsync("session-token");
                    NotifyAuthenticationStateChanged(Task.FromResult(_anonymous));
                    return _anonymous;
                }
            }
            else
            {
                var removeTokenResult = await _storageUtility.RemoveItemFromStorageAsync("session-token");
                NotifyAuthenticationStateChanged(Task.FromResult(_anonymous));
                return _anonymous;
            }

        }

        _tokenContainer.SetToken(storedToken.Token);

        return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity(claimsPrincipal, "jwtAuthType", nameType: ClaimTypes.Name, roleType: ClaimTypes.Role)));

    }

    public async Task NotifyUserLogout()
    {
        var removeTokenStatus = await _storageUtility.RemoveItemFromStorageAsync("session-token");
        var authState = Task.FromResult(_anonymous);

        NotifyAuthenticationStateChanged(authState);
    }
}
