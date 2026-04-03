using ProductManagementSystem.Shared.DataTransferObjects.Authentication;
using ProductManagementSystem.Shared.DataTransferObjects.Response;

namespace ProductManagementSystem.Api.Services.Contracts;

public interface IAuthenticationService
{
    Task<GenericResponse<TokenDto>> LoginAsync(LoginUserDto loginUser);
    Task<GenericResponse<TokenDto>> RefreshTokenAsync(TokenDto tokenDto);
    Task<GenericResponse<string>> ChangePasswordAsync(ChangePasswordDto changePasswordDto);
    Task<GenericResponse<string>> SendOtpAsync(SendOtpRequestDto sendOtpRequest);
    Task<GenericResponse<string>> ValidateOtpAsync(ValidateOtpRequestDto validateOtpRequest);
    Task<GenericResponse<string>> UnlockUserAsync(UnlockUserDto unlockUserDetails);
}
