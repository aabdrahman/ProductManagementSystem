using Microsoft.AspNetCore.Mvc;
using ProductManagementSystem.Api.Services.Contracts;
using ProductManagementSystem.Shared.DataTransferObjects.User;
using Serilog;
using System.Net;

namespace ProductManagementSystem.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class UserController : ControllerBase
{
    private readonly IUserService _userService;

    private string _methodName = "MethodName";
    private string _className = "ClassName";

    public UserController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var result = await _userService.GetAllAsync();

            return StatusCode((int)result.StatusCode, result);
        }
        catch (Exception ex)
        {
            Log.ForContext(_className, "UserController").ForContext(_methodName, "GetAll").Error(ex, "Error Invoking Ednpoint");
            return StatusCode((int)HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    [HttpGet("{Id:int}")]
    public async Task<IActionResult> GetById(int Id)
    {
        try
        {
            var result = await _userService.GetUserByIdAsync(Id);

            return StatusCode((int)result.StatusCode, result);
        }
        catch (Exception ex)
        {
            Log.ForContext(_className, "UserController").ForContext(_methodName, "GetById").Error(ex, "Error Invoking Ednpoint");
            return StatusCode((int)HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    [HttpDelete("{Id:int}")]
    public async Task<IActionResult> Delete(int Id, bool isSoftDelete = true)
    {
        try
        {
            var result = await _userService.DeleteAsync(Id, isSoftDelete);

            return StatusCode((int)result.StatusCode, result);
        }
        catch (Exception ex)
        {
            Log.ForContext(_className, "UserController").ForContext(_methodName, "Delete").Error(ex, "Error Invoking Ednpoint");
            return StatusCode((int)HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    [HttpPost]
    public async Task<IActionResult> Register([FromBody] CreateUserDto createUser)
    {
        try
        {
            var result = await _userService.CreateAsync(createUser);

            return StatusCode((int)result.StatusCode, result);
        }
        catch (Exception ex)
        {
            Log.ForContext(_className, "UserController").ForContext(_methodName, "Register").Error(ex, "Error Invoking Ednpoint");
            return StatusCode((int)HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] UpdateUserDto updateUser)
    {
        try
        {
            var result = await _userService.UpdateAsync(updateUser);

            return StatusCode((int)result.StatusCode, result);
        }
        catch (Exception ex)
        {
            Log.ForContext(_className, "UserController").ForContext(_methodName, "Update").Error(ex, "Error Invoking Ednpoint");
            return StatusCode((int)HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    [HttpPatch("confirm-user-profile")]
    public async Task<IActionResult> ConfirmUserProfile([FromBody] UpdateUserConfimationStatusDto updateUserConfimationStatus)
    {
        try
        {
            var result = await _userService.ConfirmUserAsync(updateUserConfimationStatus);

            return StatusCode((int)result.StatusCode, result);
        }
        catch (Exception ex)
        {
            Log.ForContext(_className, nameof(AuthenticationController)).ForContext(_methodName, nameof(ConfirmUserProfile)).Error(ex, "Error Invoking Ednpoint");
            return StatusCode((int)HttpStatusCode.InternalServerError, ex.Message);
        }
    }
}
