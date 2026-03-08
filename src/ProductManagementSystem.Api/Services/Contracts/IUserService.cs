using ProductManagementSystem.Shared.DataTransferObjects.Response;
using ProductManagementSystem.Shared.DataTransferObjects.User;

namespace ProductManagementSystem.Api.Services.Contracts;

public interface IUserService
{
    Task<GenericResponse<UserDto>> CreateAsync(CreateUserDto createUser);
    Task<GenericResponse<string>> DeleteAsync(int Id, bool isSoftDelete = true);
    Task<GenericResponse<UserDto>> UpdateAsync(UpdateUserDto updateUser);
    Task<GenericResponse<UserDto>> GetUserByIdAsync(int Id);
    Task<GenericResponse<IEnumerable<UserDto>>> GetAllAsync();
}
