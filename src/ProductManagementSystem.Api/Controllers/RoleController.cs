using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProductManagementSystem.Api.Services.Contracts;
using Serilog;
using System.Net;

namespace ProductManagementSystem.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class RoleController : ControllerBase
{
    private readonly IRoleService _roleService;
    private string _methodName = "MethodName";
    private string _className = "ClassName";

    public RoleController(IRoleService roleService)
    {
        _roleService = roleService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var result = await _roleService.GetAllAsync();

            return StatusCode((int)result.StatusCode, result);
        }
        catch (Exception ex)
        {
            Log.ForContext(_className, "RoleController").ForContext(_methodName, "GetAll").Error(ex, "Error Invoking Ednpoint");
            return StatusCode((int)HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    [HttpDelete("{Id:int}")]
    public async Task<IActionResult> Delete(int Id, bool isSoftDelete = true)
    {
        try
        {
            var result = await _roleService.DeleteAsync(Id, isSoftDelete);

            return StatusCode((int)result.StatusCode, result);
        }
        catch (Exception ex)
        {
            Log.ForContext(_className, "RoleController").ForContext(_methodName, "Delete").Error(ex, "Error Invoking Ednpoint");
            return StatusCode((int)HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] string roleName)
    {
        try
        {
            var result = await _roleService.CreateRoleAsync(roleName);

            return StatusCode((int)result.StatusCode, result);
        }
        catch (Exception ex)
        {
            Log.ForContext(_className, "RoleController").ForContext(_methodName, "Create").Error(ex, "Error Invoking Ednpoint");
            return StatusCode((int)HttpStatusCode.InternalServerError, ex.Message);
        }
    }
}
