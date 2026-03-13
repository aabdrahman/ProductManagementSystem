namespace ProductManagementSystem.Shared.DataTransferObjects;

public record class TokenDto
{
    public string Token { get; set; }
    public string RefreshToken { get; set; }
    public DateTime? TokenExpirationTime { get; set; }
}
