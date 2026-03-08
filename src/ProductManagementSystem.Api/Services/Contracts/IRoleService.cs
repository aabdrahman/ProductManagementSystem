using ProductManagementSystem.Shared.DataTransferObjects.Response;
using ProductManagementSystem.Shared.DataTransferObjects.Role;

namespace ProductManagementSystem.Api.Services.Contracts;

public interface IRoleService
{
    Task<GenericResponse<RoleDto>> CreateRoleAsync(string roleName);
    Task<GenericResponse<IEnumerable<RoleDto>>> GetAllAsync();
    Task<GenericResponse<string>> DeleteAsync(int Id, bool isSoftDelete = true);
}
