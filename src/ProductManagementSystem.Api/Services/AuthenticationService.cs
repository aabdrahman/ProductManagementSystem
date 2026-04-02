using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using ProductManagementSystem.Api.Data;
using ProductManagementSystem.Api.Entities.ConfigurationModels;
using ProductManagementSystem.Api.Entities.Models;
using ProductManagementSystem.Api.Helpers;
using ProductManagementSystem.Api.Services.Contracts;
using ProductManagementSystem.Api.Utilities.Contracts;
using ProductManagementSystem.Shared.DataTransferObjects.Authentication;
using ProductManagementSystem.Shared.DataTransferObjects.MailOperation;
using ProductManagementSystem.Shared.DataTransferObjects.Response;
using Serilog;
using StackExchange.Redis;
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
    private ILogger<AuthenticationService> _logger;
    private readonly IOtpOperation _otpOperation;
    private readonly IEmailService _emailService;
    private readonly OtpSettingsConfig _otpSettingsConfig;
    private readonly IRedisService _redisService;
    private readonly IDatabase _redisDatabase;

    private string _methodName = "MethodName";
    private string _className = "ClassName";

    private User? loggedInUser;

    public AuthenticationService(RepositoryContext repositoryContext, IPasswordHasher passwordHasher, IConfiguration configuration,
                                IOptionsMonitor<JwtSettingConfig> jwtSettingsOptionsMonitor, IHttpContextAccessor httpContextAccessor,
                                ILogger<AuthenticationService> logger, IOtpOperation otpOperation, IEmailService emailService, IOptionsMonitor<OtpSettingsConfig> otpSettingsConfigOptionsMonitor, 
                                IRedisService redisService, IConnectionMultiplexer connectionMultiplexer)
    {
        _repositoryContext = repositoryContext;
        _passwordHasher = passwordHasher;
        _configuration = configuration;
        _jwtSettingConfig = jwtSettingsOptionsMonitor.CurrentValue;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
        _otpOperation = otpOperation;
        _emailService = emailService;
        _otpSettingsConfig = otpSettingsConfigOptionsMonitor.CurrentValue;
        _redisService = redisService;
        _redisDatabase = connectionMultiplexer.GetDatabase();
    }
    public async Task<GenericResponse<string>> ChangePasswordAsync(ChangePasswordDto changePasswordDto)
    {
        try
        {
            Log.ForContext(_className, "AuthenticationService").ForContext(_methodName, "ChangePasswordAsync").Information("Change User Password request - {0}", changePasswordDto);

            User? userToChangePassword = await _repositoryContext.Users.IgnoreQueryFilters().SingleOrDefaultAsync(x => x.UserEmailAddress == changePasswordDto.Email.ToUpper());

            if(userToChangePassword is null)
            {
                Log.ForContext(_className, "AuthenticationService").ForContext(_methodName, "ChangePasswordAsync").Information("User Change Password Failed. User with Email does not exist - {0}", changePasswordDto.Email);
                return GenericResponse<string>.Failure("Operation Failed.", "User with Email does not exist", HttpStatusCode.NotFound);
            }

            bool isSamePassword = _passwordHasher.ValidatePassword(userToChangePassword.PasswordHash, changePasswordDto.NewPassword);

            if (isSamePassword)
            {
                Log.ForContext(_className, "AuthenticationService").ForContext(_methodName, "ChangePasswordAsync").Information("Change Password Failed. User provided same old password as new password - {0}", changePasswordDto);
                return GenericResponse<string>.Failure("Operation Failed.", "Password Change Failed. Password previously used.", HttpStatusCode.BadRequest);
            }

            userToChangePassword.PasswordHash = _passwordHasher.HashPassword(changePasswordDto.NewPassword);
            userToChangePassword.IsActive = true;

            await _repositoryContext.SaveChangesAsync();

            Log.ForContext(_className, "AuthenticationService").ForContext(_methodName, "ChangePasswordAsync").Information("User Password and Reactivation successful.");

            return GenericResponse<string>.Success("Operation Successful.", "Password Updated Successfully.", HttpStatusCode.OK);

        }
        catch (Exception ex)
        {
            Log.ForContext(_className, "AuthenticationService").ForContext(_methodName, "ChangePasswordAsync").Error(ex, "Password Chnage Failed. An Error Occurred Changing User Password.");
            return GenericResponse<string>.Failure("Operation Failed.", "An Error Occurred changing user password.", HttpStatusCode.InternalServerError, new { Message = ex.Message });
        }
    }

    public async Task<GenericResponse<TokenDto>> LoginAsync(LoginUserDto loginUser)
    {
        try
        {
            Log.ForContext(_className, "AuthenticationService").ForContext(_methodName, "LoginAsync").Information("Login Request - {0}", loginUser);

            //string origin = _httpContextAccessor.HttpContext.Request.Headers["Origin".ToString()].ToString();

            string origin = null;

            if(_httpContextAccessor.HttpContext.Request.Headers.TryGetValue("Origin", out var stringOriginValues))
            {
                origin = stringOriginValues.FirstOrDefault() ?? "";
            }

            if(!_jwtSettingConfig.ValidAudience.Split(";", StringSplitOptions.TrimEntries).Contains(origin))
            {
                Log.ForContext(_className, "AuthenticationService").ForContext(_methodName, "LoginAsync").Information("Login Failed. User is trying to signin from an unidentified origin - {0}", origin);
                return GenericResponse<TokenDto>.Failure(null, "Invalid Credentials", HttpStatusCode.BadRequest);
            }

            var isUserLockedOutInCache = await _redisDatabase.HashExistsAsync(RedisCacheHelperClass.LockedOutUsersKey, loginUser.Email.ToUpper());

            if(isUserLockedOutInCache)
            {
                Log.ForContext(_className, "AuthenticationService").ForContext(_methodName, "LoginAsync").Information("Login Failed. User is currently locked out - {0}", loginUser.Email);
                return GenericResponse<TokenDto>.Failure(null, "User account locked due to multiple failed login attempts. Kindly reset your password or contact administrator.", HttpStatusCode.BadRequest);
            }

            User? userToAuthenticate = await _repositoryContext.Users.Include(x => x.AssignedRole).SingleOrDefaultAsync(x => x.UserEmailAddress == loginUser.Email.ToUpper());

            if(userToAuthenticate is null)
            {
                Log.ForContext(_className, "AuthenticationService").ForContext(_methodName, "LoginAsync").Information("Loin Failed. User with email does not exist - {0}", loginUser.Email);
                return GenericResponse<TokenDto>.Failure(null, "Invalid Credentials", HttpStatusCode.BadRequest);
            }

            string userProfileCacheKey = RedisCacheHelperClass.GetUserProfileFailedLoginAttemptCacheKey(userToAuthenticate.Id.ToString());

            bool isPasswordCorrect = _passwordHasher.ValidatePassword(userToAuthenticate.PasswordHash, loginUser.Password);

            if (!isPasswordCorrect)
            {
                var setFailedLoginAttemptCache = await _redisDatabase.StringIncrementAsync(userProfileCacheKey, 1); //Set the current failed login attempt value to increment by 1.

                if(setFailedLoginAttemptCache >= _jwtSettingConfig.SessionLockoutAFterAttempt)
                {
                    await _redisDatabase.HashSetAsync(RedisCacheHelperClass.LockedOutUsersKey, new HashEntry[] { new HashEntry(userToAuthenticate.UserEmailAddress, true) }); //Set the user lockout cache in redis with value true.

                    userToAuthenticate.IsProfileLockedOut = true; //Lock the user account if failed login attempts are more than or equal to the lockout attempt value defined in configuration.
                    await _repositoryContext.SaveChangesAsync();
                    Log.ForContext(_className, "AuthenticationService").ForContext(_methodName, "LoginAsync").Information("User account locked due to multiple failed login attempts - {0}. Failed login attempts - {1}", userToAuthenticate, setFailedLoginAttemptCache);
                    return GenericResponse<TokenDto>.Failure(null, "User account locked due to multiple failed login attempts. Kindly reset your password or contact administrator.", HttpStatusCode.BadRequest);
                }

                Log.ForContext(_className, "AuthenticationService").ForContext(_methodName, "LoginAsync").Information("Login Failed. Invalid Password provided by user - {0}. Curent Failed login attempts - {1}", loginUser, setFailedLoginAttemptCache);

                return GenericResponse<TokenDto>.Failure(null, "Invalid Credentials", HttpStatusCode.BadRequest);
            }

            TokenDto? existingTokenDetails = await _redisService.GetItemAsync<TokenDto>(RedisCacheHelperClass.GetUserProfileTokenCacheKey(userToAuthenticate.Id.ToString(), userToAuthenticate.UserEmailAddress));


            if(existingTokenDetails is not null && existingTokenDetails.TokenExpirationTime.Value.AddSeconds(10) > DateTime.UtcNow)
            {
                Log.ForContext(_className, "AuthenticationService").ForContext(_methodName, "LoginAsync").Information("User already has an active session. Active token details - {0}", existingTokenDetails);
                return GenericResponse<TokenDto>.Success(existingTokenDetails, "User already has an active session.", HttpStatusCode.OK);
            }

            loggedInUser = userToAuthenticate;

            string token = GenerateToken();

            DateTime loginTimestamp = DateTime.UtcNow;

            loggedInUser.RefreshToken = GenerateRefreshToken();
            loggedInUser.RefreshTokenExpiryTime = DateTime.UtcNow.AddMinutes(_jwtSettingConfig.SessionTimeoutAfterMinutes);
            loggedInUser.LastLoginDate = loginTimestamp;
            loggedInUser.LastAuthenticatedDate = loginTimestamp;

            await _repositoryContext.SaveChangesAsync();

            //Set the user profile cache in redis with expiration time same as the session timeout.
            TokenDto generatedTokenToReturn = new TokenDto()
            {
                Token = token,
                RefreshToken = loggedInUser.RefreshToken,
                TokenExpirationTime = loggedInUser.RefreshTokenExpiryTime
            };

            var setTokenForUser = await _redisService.SetItemAsync<TokenDto>(generatedTokenToReturn with { TokenExpirationTime = DateTime.UtcNow.AddSeconds(_jwtSettingConfig.ExpiresAfterSeconds) }, RedisCacheHelperClass.GetUserProfileTokenCacheKey(userToAuthenticate.Id.ToString(), userToAuthenticate.UserEmailAddress), 86400);

            var removeFailedLoginAttemptCache = await _redisDatabase.KeyDeleteAsync(userProfileCacheKey); //Remove the failed login attempt cache as user successfully logged in.

            Log.ForContext(_className, "AuthenticationService").ForContext(_methodName, "LoginAsync").Information("User logged in successfully - {0}. Failed login attempts removal - {1}. Set User Token To Cache - {2}", loggedInUser, removeFailedLoginAttemptCache, setTokenForUser);

            return GenericResponse<TokenDto>.Success(generatedTokenToReturn, "User Successfully logged in.", HttpStatusCode.OK);

        }
        catch (Exception ex)
        {
            Log.ForContext(_className, "AuthenticationService").ForContext(_methodName, "LoginAsync").Error(ex, "Login Failed. An Error Occurred Loggin User In.");
            return GenericResponse<TokenDto>.Failure(null,"An Error Occurred validating credentials.", HttpStatusCode.InternalServerError, new { Message = ex.Message });
        }
    }

    public async Task<GenericResponse<TokenDto>> RefreshTokenAsync(TokenDto tokenDto)
    {
        try
        {
            Log.ForContext(_className, "AuthenticationService").ForContext(_methodName, "RefreshTokenAsync").Information("Refresh Token request - {0}", tokenDto.Token);

            var tokenPrincipals = GetPrincipalFromToken(tokenDto.Token);

            if(tokenPrincipals is null)
            {
                Log.ForContext(_className, "AuthenticationService").ForContext(_methodName, "RefreshTokenAsync").Information("Token Principal could not be fetched from the token");
                return GenericResponse<TokenDto>.Failure(null, "Invalid Token provided.", HttpStatusCode.BadRequest);
            }

            string? userEmail = tokenPrincipals.FindFirst(x => x.Type.EndsWith("emailaddress"))?.Value;

            if(userEmail is null)
            {
                Log.ForContext(_className, "AuthenticationService").ForContext(_methodName, "RefreshTokenAsync").Information("User Email could not be fetched from the token principal");
                return GenericResponse<TokenDto>.Failure(null, "Invalid Token provided.", HttpStatusCode.BadRequest);
            }

            var isUserLockedOutInCache = await _redisDatabase.HashExistsAsync(RedisCacheHelperClass.LockedOutUsersKey, userEmail.ToUpper());

            if (isUserLockedOutInCache)
            {
                Log.ForContext(_className, "AuthenticationService").ForContext(_methodName, "RefreshTokenAsync").Information("Refresh token Failed. User is currently locked out - {0}", userEmail);
                return GenericResponse<TokenDto>.Failure(null, "User account locked due to multiple failed login attempts. Kindly reset your password or contact administrator.", HttpStatusCode.BadRequest);
            }

            User? userWithToken = await _repositoryContext.Users.Include(x => x.AssignedRole).SingleOrDefaultAsync(x => x.UserEmailAddress == userEmail.ToUpper());

            if(userWithToken is null)
            {
                Log.ForContext(_className, "AuthenticationService").ForContext(_methodName, "RefreshTokenAsync").Information("Invalid User Email. User with Email could not be fetched - {0}", userEmail);
                return GenericResponse<TokenDto>.Failure(null, "Invalid Credentials.", HttpStatusCode.NotFound);
            }

            string userProfileFailedLoginAttemptCacheKey = RedisCacheHelperClass.GetUserProfileFailedLoginAttemptCacheKey(userWithToken.Id.ToString());

            if (!userWithToken.RefreshToken.Equals(tokenDto.RefreshToken) || DateTime.UtcNow > userWithToken.RefreshTokenExpiryTime)
            {
                var setFailedLoginAttemptCache = await _redisDatabase.StringIncrementAsync(userProfileFailedLoginAttemptCacheKey, 1); //Set the current failed login attempt value to increment by 1.

                if (setFailedLoginAttemptCache >= _jwtSettingConfig.SessionLockoutAFterAttempt)
                {
                    await _redisDatabase.HashSetAsync(RedisCacheHelperClass.LockedOutUsersKey, new HashEntry[] { new HashEntry(userWithToken.UserEmailAddress, true) }); //Set the user lockout cache in redis with value true.

                    //userToAuthenticate.IsActive = false; //Lock the user account if failed login attempts are more than or equal to the lockout attempt value defined in configuration.
                    //await _repositoryContext.SaveChangesAsync();
                    Log.ForContext(_className, "AuthenticationService").ForContext(_methodName, "LoginAsync").Information("User account locked due to multiple failed login attempts - {0}. Failed login attempts - {1}", userWithToken, setFailedLoginAttemptCache);
                    return GenericResponse<TokenDto>.Failure(null, "User account locked due to multiple failed login attempts. Kindly reset your password or contact administrator.", HttpStatusCode.BadRequest);
                }

                Log.ForContext(_className, "AuthenticationService").ForContext(_methodName, "RefreshTokenAsync").Information("User Refresh Token already expired or invalid refresh token provided - {0}. Set Failed login attempts - {1}", tokenDto.RefreshToken, setFailedLoginAttemptCache);
                return GenericResponse<TokenDto>.Failure(null, "Invalid Credentials.", HttpStatusCode.NotFound);
            }

            userWithToken.RefreshToken = GenerateRefreshToken();
            userWithToken.LastAuthenticatedDate = DateTime.UtcNow;

            await _repositoryContext.SaveChangesAsync();

            loggedInUser = userWithToken;

            string token = GenerateToken();

            Log.ForContext(_className, "AuthenticationService").ForContext(_methodName, "RefreshTokenAsync").Information("User refresh token generated and token pending geenration.");

            TokenDto tokenDetails = new TokenDto()
            {
                RefreshToken = userWithToken.RefreshToken,
                Token = token,
                TokenExpirationTime = userWithToken.RefreshTokenExpiryTime
            };

            var setTokenForUser = await _redisService.SetItemAsync<TokenDto>(tokenDetails with { TokenExpirationTime = DateTime.UtcNow.AddSeconds(_jwtSettingConfig.ExpiresAfterSeconds) }, RedisCacheHelperClass.GetUserProfileTokenCacheKey(userWithToken.Id.ToString(), userWithToken.UserEmailAddress), 86400);

            Log.ForContext(_className, "AuthenticationService").ForContext(_methodName, "RefreshTokenAsync").Information("Token refreshed and new access token geenrated successfully - {0}. Set Token to cache - {1}", tokenDetails,setTokenForUser);

            return GenericResponse<TokenDto>.Success(tokenDetails, "Token refreshed successfully.", HttpStatusCode.OK);

        }
        catch (Exception ex)
        {
            Log.ForContext(_className, "AuthenticationService").ForContext(_methodName, "RefreshTokenAsync").Error(ex, "An Error Occurred Refreshing Token.");
            return GenericResponse<TokenDto>.Failure(null, "An Error Occurred refreshing token details.", HttpStatusCode.InternalServerError, new { Message = ex.Message });
        }
    }

    public async Task<GenericResponse<string>> SendOtpAsync(SendOtpRequestDto sendOtpRequest)
    {
        try
        {
            Log.ForContext(_className, "AuthenticationService").ForContext(_methodName, "SendOtpAsync").Information("Generate and send OTP request - {0}", sendOtpRequest);

            var userExists = await _repositoryContext.Users.IgnoreQueryFilters().AnyAsync(x => x.UserEmailAddress == sendOtpRequest.UserEmailAddress.ToUpper() && x.Id == sendOtpRequest.UserId);

            if(!userExists)
            {
                Log.ForContext(_className, "AuthenticationService").ForContext(_methodName, "SendOtpAsync").Information("OTP Generation Failed. User with email does not exist - {0}", sendOtpRequest.UserEmailAddress);
                return GenericResponse<string>.Failure("Operation Failed.", "User with email does not exist.", HttpStatusCode.NotFound);
            }

            string generatedOTP = _otpOperation.GenerateOtp();

            if(string.IsNullOrEmpty(generatedOTP))
            {
                Log.ForContext(_className, "AuthenticationService").ForContext(_methodName, "SendOtpAsync").Information("OTP generation Failed. Generated OTP is empty - {0}", generatedOTP);
                return GenericResponse<string>.Failure("Operation Failed.", "OPT could not be generated.", HttpStatusCode.BadRequest);
            }

            UserOtpVerification userOtpVerificationToInsert = new UserOtpVerification()
            {
                UserEmail = sendOtpRequest.UserEmailAddress,
                GeneratedOTP = _passwordHasher.HashPassword(generatedOTP)
            };

            await _repositoryContext.AddAsync(userOtpVerificationToInsert);

            try
            {
                await _repositoryContext.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {

                Log.ForContext(_className, "AuthenticationService").ForContext(_methodName, "SendOtpAsync").Error(ex, "An Error occurred inserting otp record to database. Revoke Email sent result.");

                return GenericResponse<string>.Failure("Operation Failed.", "An Error Occurred sending OTP. Kindly retry.", HttpStatusCode.InternalServerError);
            }

            Dictionary<string, string> parameters = new Dictionary<string, string>();

            parameters.Add("OTP_CODE", generatedOTP);
            parameters.Add("UserEmail", sendOtpRequest.UserEmailAddress);

            string mailContent = EmailContentHelper.GetMailContent("SendOtpTemplate.html", parameters);

            if(!string.IsNullOrEmpty(mailContent))
            {
                EmailSenderDto emailDetails = new EmailSenderDto(Subject: "Account Profile Verification", Content: mailContent, [sendOtpRequest.UserEmailAddress.ToUpper()], isHtml: true);

                bool isEmailSent = await _emailService.SendEmailAsync(emailDetails);

                if (!isEmailSent)
                {
                    Log.ForContext(_className, "AuthenticationService").ForContext(_methodName, "SendOtpAsync").Information("Email could not be sent to user.");
                    return GenericResponse<string>.Failure("Operation Failed.", "OTP send failed. Kindly retry again.", HttpStatusCode.BadRequest);
                }

            }

            Log.ForContext(_className, "AuthenticationService").ForContext(_methodName, "SendOtpAsync").Information("OTP Generated and sent successfully - {0}", userOtpVerificationToInsert);

            return GenericResponse<string>.Success("Operation Successful.", "OTP generated and sent successfully.", HttpStatusCode.OK);

        }
        catch (Exception ex)
        {
            Log.ForContext(_className, "AuthenticationService").ForContext(_methodName, "SendOtpAsync").Error(ex, "An Error Occurred generating and sending OTP to user.");
            return GenericResponse<string>.Failure("Operation Failed.", "An Error Occurred generating and sending OTP to user.", HttpStatusCode.InternalServerError, new { Message = ex.Message });
        }
    }

    public async Task<GenericResponse<string>> ValidateOtpAsync(ValidateOtpRequestDto validateOtpRequest)
    {
        try
        {
            Log.ForContext(_className, "AuthenticationService").ForContext(_methodName, "ValidateOtpAsync").Information("Validate OTP request - {0}", validateOtpRequest);

            UserOtpVerification? userOtp = await _repositoryContext.UserOtpVerifications.Include(x => x.UserToConfirmDetails)
                                                                            .OrderByDescending(x => x.CreatedAt)
                                                                            .FirstOrDefaultAsync(x => x.UserEmail == validateOtpRequest.UserEmailAddress.ToUpper());

            if(userOtp is null)
            {
                Log.ForContext(_className, "AuthenticationService").ForContext(_methodName, "ValidateOtpAsync").Information("OTP Validation Failed. No OTP exists for provided user - {0}", validateOtpRequest);
                return GenericResponse<string>.Failure("Operation Failed.", "OTP Verification Failed. Kindly use the resend option.", HttpStatusCode.NotFound);
            }


            if(userOtp.CreatedAt <= DateTime.UtcNow.AddMinutes(0 - _otpSettingsConfig.ExpiresAfterMinutes))
            {
                Log.ForContext(_className, "AuthenticationService").ForContext(_methodName, "ValidateOtpAsync").Information("OTP Validation Failed. Existing OTP already expired. Expires At: {0}", userOtp.CreatedAt.AddMinutes(_otpSettingsConfig.ExpiresAfterMinutes));
                return GenericResponse<string>.Failure("Operation Failed.", "OTP Verification Failed. OTP already expired.", HttpStatusCode.BadRequest);
            }

            bool isValidOTP = _passwordHasher.ValidatePassword(userOtp.GeneratedOTP, validateOtpRequest.OTP); 

            if (!isValidOTP)
            {
                Log.ForContext(_className, "AuthenticationService").ForContext(_methodName, "ValidateOtpAsync").Information("OTP Validation Failed. Invalid OTP provided.");
                return GenericResponse<string>.Failure("Operation Failed.", "OTP Verification Failed. Invalid OTP provided.", HttpStatusCode.BadRequest);
            }

            //int affectedRows = await _repositoryContext.Users.Where(x => x.UserEmailAddress == userOtp.UserEmail.ToUpper())
            //                                                .ExecuteUpdateAsync(x => x.SetProperty(x => x.ConfirmedAt, DateTime.UtcNow).SetProperty(x => x.IsUserConfirmed, true));

            //Log.ForContext(_className, "AuthenticationService").ForContext(_methodName, "ValidateOtpAsync").Information("User marked as confirmed successfully. Query returns - {0}", affectedRows);

            userOtp.UserToConfirmDetails.IsUserConfirmed = true;
            userOtp.UserToConfirmDetails.ConfirmedAt = DateTime.UtcNow;
            _repositoryContext.UserOtpVerifications.Remove(userOtp);

            await _repositoryContext.SaveChangesAsync();

            Log.ForContext(_className, "AuthenticationService").ForContext(_methodName, "ValidateOtpAsync").Information("OTP Validated Successfully. Record removed.");

            return GenericResponse<string>.Success("Operation Failed.", "OTP Verification Successful.", HttpStatusCode.OK);
        }
        catch (Exception ex)
        {
            Log.ForContext(_className, "AuthenticationService").ForContext(_methodName, "ValidateOtpAsync").Error(ex, "An Error Occurred Validating OTP.");
            return GenericResponse<string>.Failure("Operation Failed.", "An Error Occurred Validating OTP.", HttpStatusCode.InternalServerError, new { Message = ex.Message });
        }
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
            expires: DateTime.UtcNow.AddSeconds(_jwtSettingConfig.ExpiresAfterSeconds),
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

    private ClaimsPrincipal? GetPrincipalFromToken(string token)
    {
        string secretKey = Environment.GetEnvironmentVariable("PmsSECRET") ?? throw new ArgumentNullException("Cannot proceed as secret key could not be fetched.");

        TokenValidationParameters tokenValidationParamter = new TokenValidationParameters()
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,

            ValidAudiences = _jwtSettingConfig.ValidAudience?.Split(";", StringSplitOptions.RemoveEmptyEntries) ?? [],
            ValidIssuer = _jwtSettingConfig.ValidIssuer,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
        };

        SecurityToken securityToken;

        var tokenHandler = new JwtSecurityTokenHandler();

        var principal = tokenHandler.ValidateToken(token, tokenValidationParamter, out securityToken);

        var jwtSecurityToken = securityToken as JwtSecurityToken;

        if(jwtSecurityToken is null || !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.CurrentCultureIgnoreCase))
        {
            return null;    
        }

        return principal;
    }
}
