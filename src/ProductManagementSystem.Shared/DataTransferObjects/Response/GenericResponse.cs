using System.Net;

namespace ProductManagementSystem.Shared.DataTransferObjects.Response;

public class GenericResponse<T>
{
    public T Data { get; set; }
    public HttpStatusCode StatusCode { get; set; }
    public string ResponseMessage { get; set; }
    public bool IsSuccessStatus { get; set; }
    public object ErrorDetails { get; set; }

    public GenericResponse(T? data, string responseMessage, bool isSuccessStatus, HttpStatusCode statusCode, object errorDetails)
    {
        this.Data = data;
        this.ResponseMessage = responseMessage;
        this.StatusCode = statusCode;
        this.IsSuccessStatus = isSuccessStatus;
        this.ErrorDetails = errorDetails;
    }

    public static GenericResponse<T> Success(T entityData, string message, HttpStatusCode httpStatusCode) => new GenericResponse<T>(entityData, message, true, httpStatusCode, null);
    public static GenericResponse<T> Failure(T? entity, string message, HttpStatusCode httpStatusCode, object error = null) => new GenericResponse<T>(entity, message, false, httpStatusCode, error);
}
