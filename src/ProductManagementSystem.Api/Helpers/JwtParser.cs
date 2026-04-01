using System.Security.Claims;
using System.Text.Json;

namespace ProductManagementSystem.Client.Utilities;

public static class JwtParser
{

    public static IEnumerable<Claim> ParseClaimsFromJwt(string jwtToken)
    {
        var claims = new List<Claim>();
        var jwtPayload = jwtToken.Split(".")[1];

        var jwtJsonBytes = ParseBase64WithoutPadding(jwtPayload);

        var keyValuePairs = JsonSerializer.Deserialize<Dictionary<string, object>>(jwtJsonBytes);

        claims.AddRange(keyValuePairs.Select(kvp => new Claim(kvp.Key, kvp.Value.ToString())));

        return claims;
    }


    private static byte[] ParseBase64WithoutPadding(string base64String)
    {
        switch (base64String.Length % 4)
        {
            case 2: base64String += "=="; break;
            case 3: base64String += "="; break;
        }

        return Convert.FromBase64String(base64String);
    }
}
