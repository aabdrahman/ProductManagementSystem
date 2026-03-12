using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProductManagementSystem.Api.Services.Contracts;
using ProductManagementSystem.Shared.DataTransferObjects.Feedback;
using Serilog;
using System.Net;

namespace ProductManagementSystem.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class FeedbackController : ControllerBase
{
    private readonly IFeedbackService _feedbackService;

    private string _methodName = "MethodName";
    private string _className = "ClassName";

    public FeedbackController(IFeedbackService feedbackService)
    {
        _feedbackService = feedbackService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var result = await _feedbackService.GetAllFeedbacksAsync();

            return StatusCode((int)result.StatusCode, result);
        }
        catch (Exception ex)
        {
            Log.ForContext(_className, "FeedbackController").ForContext(_methodName, "GetAll").Error(ex, "Error Invoking Ednpoint");
            return StatusCode((int)HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    [HttpPost]
    public async Task<IActionResult> CreateFeedback([FromBody] CreateFeedbackDto createFeedbackDto)
    {
        try
        {
            var result = await _feedbackService.CreateFeedbackAsync(createFeedbackDto);
            return StatusCode((int)result.StatusCode, result);
        }
        catch (Exception ex)
        {
            Log.ForContext(_className, "FeedbackController").ForContext(_methodName, "CreateFeedback").Error(ex, "Error Invoking Ednpoint");
            return StatusCode((int)HttpStatusCode.InternalServerError, ex.Message);
        }
    }
}
