using System.Net;

namespace ProductManagementSystem.Shared.DataTransferObjects.Response;

public class GenericResponse<TEntity>
{
    public TEntity Data { get; set; }
    public HttpStatusCode StatusCode { get; set; }
    public string ResponseMessage { get; set; }
    public bool IsSuccessStatus { get; set; }
    public object ErrorDetails { get; set; }

    public GenericResponse(TEntity? data, string message, bool isSuccessful, HttpStatusCode httpStatus, object errorDetails)
    {
        Data = data;
        ResponseMessage = message;
        StatusCode = httpStatus;
        IsSuccessStatus = isSuccessful;
        ErrorDetails = errorDetails;
    }

    public static GenericResponse<TEntity> Success(TEntity entityData, string message, HttpStatusCode httpStatusCode) => new GenericResponse<TEntity>(entityData, message, true, httpStatusCode, null);
    public static GenericResponse<TEntity> Failure(TEntity? entity, string message, HttpStatusCode httpStatusCode, object error = null) => new GenericResponse<TEntity>(entity, message, false, httpStatusCode, error);
}
