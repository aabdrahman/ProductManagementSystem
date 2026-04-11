using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProductManagementSystem.Api.Services.Contracts;
using ProductManagementSystem.Shared.DataTransferObjects.ContentDetails;
using Serilog;

namespace ProductManagementSystem.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "ADMIN,SYSTEM")]
public class AboutUsController : ControllerBase
{
    private readonly IAboutUsService _aboutUsService;

    private string _methodName = "MethodName";
    private string _className = "ClassName";

    public AboutUsController(IAboutUsService aboutUsService)
    {
        _aboutUsService = aboutUsService;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var result = await _aboutUsService.GetAllAsync(new CancellationToken());

            return StatusCode((int)result.StatusCode, result);
        }
        catch (Exception ex)
        {
            Log.ForContext(_className, nameof(AboutUsController)).ForContext(_methodName, nameof(GetAll)).Error(ex, "An error occurred invoking endpoint");
            return StatusCode(500, ex.Message);
        }
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAboutUsDto createAboutUs)
    {
        try
        {
            var result = await _aboutUsService.CreateAsync(createAboutUs, new CancellationToken());

            return StatusCode((int)result.StatusCode,result);
        }
        catch (Exception ex)
        {
            Log.ForContext(_className, nameof(AboutUsController)).ForContext(_methodName, nameof(Create)).Error(ex, "An error occurred invoking endpoint");
            return StatusCode(500, ex.Message);
        }
    }

    [HttpDelete("{Id:guid}")]
    public async Task<IActionResult> Delete(Guid Id, bool isSoftDelete = true)
    {
        try
        {
            var result = await _aboutUsService.DeleteAsync(Id, isSoftDelete, new CancellationToken());

            return StatusCode((int)result.StatusCode, result);
        }
        catch (Exception ex)
        {
            Log.ForContext(_className, nameof(AboutUsController)).ForContext(_methodName, nameof(Delete)).Error(ex, "An error occurred invoking endpoint");
            return StatusCode(500, ex.Message);
        }
    }

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] UpdateAboutUsDto updateAboutUs)
    {
        try
        {
            var result = await _aboutUsService.UpdateAsync(updateAboutUs, new CancellationToken());

            return StatusCode((int)result.StatusCode, result);
        }
        catch (Exception ex)
        {
            Log.ForContext(_className, nameof(AboutUsController)).ForContext(_methodName, nameof(Update)).Error(ex, "An error occurred invoking endpoint");
            return StatusCode(500, ex.Message);
        }
    }
}
