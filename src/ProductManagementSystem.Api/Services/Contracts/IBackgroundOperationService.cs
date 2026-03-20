using ProductManagementSystem.Shared.DataTransferObjects.Response;

namespace ProductManagementSystem.Api.Services.Contracts;

public interface IBackgroundOperationService
{
    Task<GenericResponse<string>> RemoveExpiredOTPAsync();
}
