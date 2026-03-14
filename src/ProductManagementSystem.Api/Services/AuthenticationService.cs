using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using ProductManagementSystem.Api.Data;
using ProductManagementSystem.Api.Entities.ConfigurationModels;
using ProductManagementSystem.Api.Entities.Models;
using ProductManagementSystem.Api.Services.Contracts;
using ProductManagementSystem.Api.Utilities.Contracts;
using ProductManagementSystem.Shared.DataTransferObjects.Authentication;
using ProductManagementSystem.Shared.DataTransferObjects.Response;
using Serilog;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace ProductManagementSystem.Api.Services;

public class AuthenticationService : IAuthenticationService
{
    private readonly RepositoryContext _repositoryContext;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IConfiguration _configuration;
    private readonly JwtSettingConfig _jwtSettingConfig;
    private readonly IHttpContextAccessor _httpContextAccessor;

    private string _methodName = "MethodName";
    private string _className = "ClassName";

    private User? loggedInUser;

    public AuthenticationService(RepositoryContext repositoryContext, IPasswordHasher passwordHasher, IConfiguration configuration, IOptionsMonitor<JwtSettingConfig> jwtSettingsOptionsMonitor, IHttpContextAccessor httpContextAccessor)
    {
        _repositoryContext = repositoryContext;
        _passwordHasher = passwordHasher;
        _configuration = configuration;
        _jwtSettingConfig = jwtSettingsOptionsMonitor.CurrentValue;
        _httpContextAccessor = httpContextAccessor;
    }
    public Task<GenericResponse<string>> ChangePasswordAsync(ChangePasswordDto changePasswordDto)
    {
        throw new NotImplementedException();
    }

    public async Task<GenericResponse<TokenDto>> LoginAsync(LoginUserDto loginUser)
    {
        try
        {
            Log.ForContext(_className, "AuthenticationService").ForContext(_methodName, "LoginAsync").Information("Login Request - {0}", loginUser);

            string origin = _httpContextAccessor.HttpContext.Request.Headers["Origin".ToString()].ToString();

            if(!_jwtSettingConfig.ValidAudience.Split(";", StringSplitOptions.TrimEntries).Contains(origin))
            {
                Log.ForContext(_className, "AuthenticationService").ForContext(_methodName, "LoginAsync").Information("Login Failed. User is trying to signin from an unidentified origin - {0}", origin);
                return GenericResponse<TokenDto>.Failure(null, "Invalid Credentials", HttpStatusCode.BadRequest);
            }

            User? userToAuthenticate = await _repositoryContext.Users.Include(x => x.AssignedRole).SingleOrDefaultAsync(x => x.UserEmailAddress == loginUser.Email.ToUpper());

            if(userToAuthenticate is null)
            {
                Log.ForContext(_className, "AuthenticationService").ForContext(_methodName, "LoginAsync").Information("Loin Failed. User with email does not exist - {0}", loginUser.Email);
                return GenericResponse<TokenDto>.Failure(null, "Invalid Credentials", HttpStatusCode.BadRequest);
            }

            bool isPasswordCorrect = _passwordHasher.ValidatePassword(userToAuthenticate.PasswordHash, loginUser.Password);

            if (isPasswordCorrect)
            {
                Log.ForContext(_className, "AuthenticationService").ForContext(_methodName, "LoginAsync").Information("Login Failed. Invalid Password provided by user - {0}", loginUser);
                return GenericResponse<TokenDto>.Failure(null, "Invalid Credentials", HttpStatusCode.BadRequest);
            }

            loggedInUser = userToAuthenticate;

            string token = GenerateToken();

            loggedInUser.RefreshToken = GenerateRefreshToken();
            loggedInUser.RefreshTokenExpiryTime = DateTime.UtcNow.AddMinutes(_jwtSettingConfig.SessionTimeoutAfterMinutes);

            await _repositoryContext.SaveChangesAsync();

            Log.ForContext(_className, "AuthenticationService").ForContext(_methodName, "LoginAsync").Information("User logged in successfully - {0}", loggedInUser);

            return GenericResponse<TokenDto>.Success(new TokenDto()
            {
                Token = token,
                RefreshToken = loggedInUser.RefreshToken,
                TokenExpirationTime = loggedInUser.RefreshTokenExpiryTime
            }, "User Successfully logged in.", HttpStatusCode.OK);

        }
        catch (Exception ex)
        {
            Log.ForContext(_className, "AuthenticationService").ForContext(_methodName, "LoginAsync").Error(ex, "Login Failed. An Error Occurred Loggin User In.");
            return GenericResponse<TokenDto>.Failure(null,"An Error Occurred validating credentials.", HttpStatusCode.InternalServerError, new { Message = ex.Message });
        }
    }

    public Task<GenericResponse<TokenDto>> RefreshTokenAsync(TokenDto tokenDto)
    {
        throw new NotImplementedException();
    }

    private List<Claim> GetClaims()
    {
        return new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, loggedInUser.Id.ToString()),
        new Claim(ClaimTypes.Name, $"{loggedInUser.FirstName} {loggedInUser.LastName}"),
        new Claim(ClaimTypes.Email, loggedInUser.UserEmailAddress),
        new Claim(ClaimTypes.Role, loggedInUser.AssignedRole.NormalizedName),

        new Claim(JwtRegisteredClaimNames.Sub, loggedInUser.Id.ToString()),
        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
    };
    }

    private SigningCredentials GetCredentials()
    {
        string secretKey = Environment.GetEnvironmentVariable("PmsSECRET") ?? throw new ArgumentNullException("Cannot proceed as secret key could not be fetched.");

        var encodedKey = Encoding.UTF8.GetBytes(secretKey);

        var securityKey = new SymmetricSecurityKey(encodedKey);

        return new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
    }

    private JwtSecurityToken GetTokenOptions(List<Claim> claims, SigningCredentials credentials)
    {
        var jwtSettings = _configuration.GetSection("JwtSettings");

        var options = new JwtSecurityToken
        (
            audience: _httpContextAccessor.HttpContext.Request.Headers["Origin"].ToString() ?? "ProjectManagementSystemAPI",
            issuer: _jwtSettingConfig.ValidIssuer,
            claims: claims,
            expires: DateTime.Now.AddSeconds(_jwtSettingConfig.ExpiresAfterSeconds),
            signingCredentials: credentials 
            
        );

        return options;
    }

    private string GenerateRefreshToken()
    {

        var rndNum = new byte[32];

        using (var randNum = RandomNumberGenerator.Create())
        {
            randNum.GetBytes(rndNum);
        }

        return Convert.ToBase64String(rndNum); ;
    }

    private string GenerateToken()
    {
        var userClaims = GetClaims();
        var signinCredentials = GetCredentials();
        var options = GetTokenOptions(userClaims, signinCredentials);

        var token = new JwtSecurityTokenHandler().WriteToken(options);

        return token;
    }
}
