using Microsoft.AspNetCore.Diagnostics;
using ProductManagementSystem.Shared.DataTransferObjects.Response;
using Serilog;

namespace ProductManagementSystem.Api;

public class GlobalExceptionHandler : IExceptionHandler
{
    private string _methodName = "MethodName";
    private string _className = "ClassName";
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        httpContext.Response.ContentType = "application/json";

        var contextFeature = httpContext.Features.Get<IExceptionHandlerFeature>();

        var errorDetails = new ErrorResponse();

        errorDetails.ErrorMessage = exception.Message;
        errorDetails.ErrorDescription = exception?.InnerException?.Message;
        errorDetails.ErrorType = exception.GetType().ToString();

        var responseBody = GenericResponse<object>.Failure(null, "An Error Occurred.", System.Net.HttpStatusCode.InternalServerError, errorDetails);

        Log.ForContext(_methodName, "TryHandleAsync").ForContext(_className, "GlobalExceptionHandler").Error(exception, "A global error occurred- Trace: {0}", exception.StackTrace);

        await httpContext.Response.WriteAsJsonAsync(responseBody);

        return true;
    }
}


internal class ErrorResponse
{
    public string ErrorType { get; set; }
    public string ErrorMessage { get; set; }
    public string? ErrorDescription { get; set; }
}