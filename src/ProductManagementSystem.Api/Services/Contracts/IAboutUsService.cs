using ProductManagementSystem.Shared.DataTransferObjects.ContentDetails;
using ProductManagementSystem.Shared.DataTransferObjects.Response;

namespace ProductManagementSystem.Api.Services.Contracts;

public interface IAboutUsService
{
    Task<GenericResponse<string>> CreateAsync(CreateAboutUsDto createAboutUs, CancellationToken cancellationToken = default);
    Task<GenericResponse<IEnumerable<AboutUsDto>>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<GenericResponse<string>> DeleteAsync(Guid Id, bool isSoftDelete = false,  CancellationToken cancellationToken = default);
    Task<GenericResponse<string>> UpdateAsync(UpdateAboutUsDto updateAboutUs, CancellationToken cancellationToken = default);
}
