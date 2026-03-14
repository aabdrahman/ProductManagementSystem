using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProductManagementSystem.Api.Services.Contracts;
using ProductManagementSystem.Shared.DataTransferObjects;
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
}
