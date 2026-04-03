using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Net.Http.Headers;
using ProductManagementSystem.Api.Helpers;
using ProductManagementSystem.Api.Utilities.Contracts;
using ProductManagementSystem.Client.Utilities;
using ProductManagementSystem.Shared.DataTransferObjects.Authentication;
using ProductManagementSystem.Shared.DataTransferObjects.Response;
using StackExchange.Redis;
using System.Security.Claims;

namespace ProductManagementSystem.Api.Controllers.ServiceFilters;

public class AuthenticationTokenValidationFilter(IRedisService redisService, IConfiguration configuration) : IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        string requestToken = string.Empty;

        var existingTokenHeaders = context.HttpContext.Request.Headers[HeaderNames.Authorization].ToString();

        if(!string.IsNullOrEmpty(existingTokenHeaders) && existingTokenHeaders.StartsWith("Bearer "))
        {
            requestToken = existingTokenHeaders.Substring("Bearer ".Length).Trim();
        }

        if(string.IsNullOrEmpty(requestToken))
        {
            context.Result = new UnauthorizedObjectResult(GenericResponse<object>.Failure(null, "Bearer Token could not be validated.", System.Net.HttpStatusCode.Unauthorized));
            return;
        }

        var userId = context.HttpContext.User.FindFirst(x => x.Type.EndsWith("nameidentifier"))?.Value ?? "";
        var userEmail = context.HttpContext.User.FindFirst(x => x.Type.EndsWith("emailaddress"))?.Value ?? "";

        int userLockedOutAttempts = await redisService.GetItemAsync<int>(userId.ToUpper());

        if(userLockedOutAttempts >= configuration.GetValue<int>("JwtSettings:SessionLockoutAFterAttempt"))
        {
            context.Result = new UnauthorizedObjectResult(GenericResponse<object>.Failure(null, "User is locked out due to multiple failed login attempts.", System.Net.HttpStatusCode.Unauthorized));
            return;
        }

        var expectedToken = await redisService.GetItemAsync<TokenDto>(RedisCacheHelperClass.GetUserProfileTokenCacheKey(userId, userEmail));

        if(expectedToken is null)
        {
            context.Result = new UnauthorizedObjectResult(GenericResponse<object>.Failure(null, "Token could not be fetched.", System.Net.HttpStatusCode.Unauthorized));
            return;
        }

        if (!expectedToken.Token.Equals(requestToken, StringComparison.OrdinalIgnoreCase))
        {
            context.Result = new UnauthorizedObjectResult(GenericResponse<object>.Failure(null, "Invalid User Token supplied", System.Net.HttpStatusCode.Unauthorized));
            return;
        }

        IEnumerable<Claim> cachedTokenOriginClaims = JwtParser.ParseClaimsFromJwt(expectedToken.Token);

        if (!cachedTokenOriginClaims.Any())
        {
            context.Result = new UnauthorizedObjectResult(GenericResponse<object>.Failure(null, "Token claim is parsing failed.", System.Net.HttpStatusCode.Unauthorized));
            return;
        }

        string tokenAudience = cachedTokenOriginClaims.FirstOrDefault(x => x.Type.Contains("aud"))?.Value ?? "";
        string sessionAudience = context.HttpContext.User.FindFirst(x => x.Type.Contains("aud"))?.Value ?? "";

        if (string.IsNullOrEmpty(tokenAudience) || string.IsNullOrEmpty(sessionAudience))
        {
            context.Result = new UnauthorizedObjectResult(GenericResponse<object>.Failure(null, "Token Audience claim is missing.", System.Net.HttpStatusCode.Unauthorized));
            return;
        }

        if(!string.Equals(tokenAudience, sessionAudience, StringComparison.OrdinalIgnoreCase))
        {
            context.Result = new UnauthorizedObjectResult(GenericResponse<object>.Failure(null, "Token Audience claim mismatched..", System.Net.HttpStatusCode.Unauthorized));
            return;
        }
    }
}
