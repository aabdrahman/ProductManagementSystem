using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ProductManagementSystem.Api.Services.Contracts;
using Serilog;
using System.Net;

namespace ProductManagementSystem.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[EnableRateLimiting("per-user-limit")]
public class UserOrderVerificationController : ControllerBase
{
    private readonly IUserOrderVerificationService _userOrderVerificationService;

    private string _methodName = "MethodName";
    private string _className = "ClassName";

    public UserOrderVerificationController(IUserOrderVerificationService userOrderVerificationService)
    {
        _userOrderVerificationService = userOrderVerificationService;
    }


    [HttpGet("tokenVerification/{verification_token}")]
    public async Task<IActionResult> VerifyToken(string verification_token)
    {
        try
        {
            var result = await _userOrderVerificationService.VerifyOrderAsync(verification_token);

            return Ok(result);
        }
        catch (Exception ex)
        {
            Log.ForContext(_className, nameof(UserOrderVerificationController)).ForContext(_methodName, nameof(VerifyToken)).Error(ex, "Error Invoking EndPoint");
            return StatusCode((int)HttpStatusCode.InternalServerError, ex.Message);
        }
    }
}
