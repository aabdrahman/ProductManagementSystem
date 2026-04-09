using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProductManagementSystem.Api.Services.Contracts;
using ProductManagementSystem.Shared.DataTransferObjects.ContentDetails;
using Serilog;

namespace ProductManagementSystem.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "ADMIN,SYSTEM")]
public class WhyChooseUsController : ControllerBase
{
    private readonly IWhyChooseUsService _whyChooseUsService;

    public WhyChooseUsController(IWhyChooseUsService whyChooseUsService)
    {
        _whyChooseUsService = whyChooseUsService;
    }

    private string _methodName = "MethodName";
    private string _className = "ClassName";

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var result = await _whyChooseUsService.GetAllAsync(new CancellationToken());

            return StatusCode((int)result.StatusCode, result);
        }
        catch (Exception ex)
        {
            Log.ForContext(_className, nameof(WhyChooseUsController)).ForContext(_methodName, nameof(GetAll)).Error(ex, "An error occurred invoking endpoint");
            return StatusCode(500, ex.Message);
        }
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateWhyChooseUsDto createWhyChooseUs)
    {
        try
        {
            var result = await _whyChooseUsService.CreateAsync(createWhyChooseUs, new CancellationToken());

            return StatusCode((int)result.StatusCode, result);
        }
        catch (Exception ex)
        {
            Log.ForContext(_className, nameof(WhyChooseUsController)).ForContext(_methodName, nameof(Create)).Error(ex, "An error occurred invoking endpoint");
            return StatusCode(500, ex.Message);
        }
    }

    [HttpDelete("{Id:guid}")]
    public async Task<IActionResult> Delete(Guid Id, bool isSoftDelete = false)
    {
        try
        {
            var result = await _whyChooseUsService.DeleteAsync(Id, isSoftDelete, new CancellationToken());

            return StatusCode((int)result.StatusCode, result);
        }
        catch (Exception ex)
        {
            Log.ForContext(_className, nameof(WhyChooseUsController)).ForContext(_methodName, nameof(Delete)).Error(ex, "An error occurred invoking endpoint");
            return StatusCode(500, ex.Message);
        }
    }

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] UpdateWhyChooseUsDto updateWhyChooseUsDto)
    {
        try
        {
            var result = await _whyChooseUsService.UpdateAsync(updateWhyChooseUsDto);

            return StatusCode((int)result.StatusCode, result);
        }
        catch (Exception ex)
        {
            Log.ForContext(_className, nameof(WhyChooseUsController)).ForContext(_methodName, nameof(Update)).Error(ex, "An error occurred invoking endpoint");
            return StatusCode(500, ex.Message);
        }
    }
}
