using System.ComponentModel.DataAnnotations;

namespace ProductManagementSystem.Shared.DataTransferObjects.Authentication;

public record class TokenDto
{
    [Required(ErrorMessage = "Access token is required.")]
    public string Token { get; set; }
    [Required(ErrorMessage = "Access token is required.")]
    public string RefreshToken { get; set; }
    public DateTime? TokenExpirationTime { get; set; }
}
