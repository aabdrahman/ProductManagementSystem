using ProductManagementSystem.Shared.DataTransferObjects.ContentDetails;
using ProductManagementSystem.Shared.DataTransferObjects.Response;

namespace ProductManagementSystem.Api.Services.Contracts;

public interface IWhyChooseUsService
{
    Task<GenericResponse<string>> CreateAsync(CreateWhyChooseUsDto createWhyChooseUs, CancellationToken cancellationToken = default);
    Task<GenericResponse<IEnumerable<WhyChooseUsDto>>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<GenericResponse<string>> DeleteAsync(Guid Id, bool isSoftDelete = false, CancellationToken cancellationToken = default);
    Task<GenericResponse<string>> UpdateAsync(UpdateWhyChooseUsDto updateWhyChooseUs, CancellationToken cancellationToken = default);
}