using Microsoft.AspNetCore.Components.Authorization;
using ProductManagementSystem.Client.Utilities;
using ProductManagementSystem.Client.Utilities.Contracts;
using ProductManagementSystem.Shared.DataTransferObjects.Authentication;
using System.Security.Claims;

namespace ProductManagementSystem.Client.AuthProviderUtility;

public class AuthStateProvider : AuthenticationStateProvider
{
    private readonly ILocalStorageUtility _storageUtility;
    private AuthenticationState _anonymous;

    public AuthStateProvider(ILocalStorageUtility storageUtility)
    {
        _storageUtility = storageUtility;
        _anonymous = new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
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
        Console.WriteLine("Expiry Timestamp - {0}", expiryTimestamp);

        if(DateTimeOffset.UtcNow > expiryTimestamp)
        {
            var removeTokenResult = await _storageUtility.RemoveItemFromStorageAsync("session-token");
            NotifyAuthenticationStateChanged(Task.FromResult(_anonymous));
            return _anonymous;
        }

        return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity(claimsPrincipal, "jwtAuthType", nameType: ClaimTypes.Name, roleType: ClaimTypes.Role)));

    }

    public async Task NotifyUserLogout()
    {
        var removeTokenStatus = await _storageUtility.RemoveItemFromStorageAsync("session-token");
        var authState = Task.FromResult(_anonymous);

        NotifyAuthenticationStateChanged(authState);
    }
}
