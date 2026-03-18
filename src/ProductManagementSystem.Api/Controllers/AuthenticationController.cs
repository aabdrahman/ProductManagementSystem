using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProductManagementSystem.Api.Services.Contracts;
using ProductManagementSystem.Shared.DataTransferObjects.Authentication;
using Serilog;
using System.Net;

namespace ProductManagementSystem.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthenticationController : ControllerBase
{
    private readonly IAuthenticationService _authenticationService;

    private string _methodName = "MethodName";
    private string _className = "ClassName";

    public AuthenticationController(IAuthenticationService authenticationService)
    {
        _authenticationService = authenticationService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginUserDto loginUser)
    {
        try
        {
            var result = await _authenticationService.LoginAsync(loginUser);

            return StatusCode((int)result.StatusCode, result);
        }
        catch (Exception ex)
        {
            Log.ForContext(_className, "AuthenticationController").ForContext(_methodName, "Login").Error(ex, "Error Invoking Ednpoint");
            return StatusCode((int)HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshToken([FromBody] TokenDto tokenDto)
    {
        try
        {
            var result = await _authenticationService.RefreshTokenAsync(tokenDto);

            return StatusCode((int)result.StatusCode, result);
        }
        catch (Exception ex)
        {
            Log.ForContext(_className, "AuthenticationController").ForContext(_methodName, "RefreshToken").Error(ex, "Error Invoking Ednpoint");
            return StatusCode((int)HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto changePasswordDto)
    {
        try
        {
            var result = await _authenticationService.ChangePasswordAsync(changePasswordDto);

            return StatusCode((int)result.StatusCode, result);
        }
        catch (Exception ex)
        {
            Log.ForContext(_className, "AuthenticationController").ForContext(_methodName, "ChangePassword").Error(ex, "Error Invoking Ednpoint");
            return StatusCode((int)HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    [HttpPost("send-otp")]
    public async Task<IActionResult> SendOtp([FromBody] SendOtpRequestDto sendOtpRequest)
    {
        try
        {
            var result = await _authenticationService.SendOtpAsync(sendOtpRequest);

            return StatusCode((int)result.StatusCode, result);
        }
        catch (Exception ex)
        {
            Log.ForContext(_className, "AuthenticationController").ForContext(_methodName, "SendOtp").Error(ex, "Error Invoking Ednpoint");
            return StatusCode((int)HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    [HttpPost("validate-otp")]
    public async Task<IActionResult> ValidateOtp([FromBody] ValidateOtpRequestDto validateOtpRequest)
    {
        try
        {
            var result = await _authenticationService.ValidateOtpAsync(validateOtpRequest);

            return StatusCode((int)result.StatusCode, result);
        }
        catch (Exception ex)
        {
            Log.ForContext(_className, "AuthenticationController").ForContext(_methodName, "ValidateOtp").Error(ex, "Error Invoking Ednpoint");
            return StatusCode((int)HttpStatusCode.InternalServerError, ex.Message);
        }
    }
}
