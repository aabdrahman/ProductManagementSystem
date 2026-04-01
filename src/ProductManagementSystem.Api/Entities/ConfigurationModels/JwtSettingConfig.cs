namespace ProductManagementSystem.Api.Entities.ConfigurationModels;

public class JwtSettingConfig
{
    public string ValidAudience { get; set; }
    public string ValidIssuer { get; set; }
    public double ExpiresAfterSeconds { get; set; }
    public double SessionTimeoutAfterMinutes { get; set; }
    public double SessionLockoutAFterAttempt { get; set; }
}
