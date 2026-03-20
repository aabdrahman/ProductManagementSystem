namespace ProductManagementSystem.Api.Entities.ConfigurationModels;

public class RemoveExpiredOtpBackgroundConfig
{
    public int RunAfterSeconds { get; set; }
    public double ExpiresAfterInMinutes { get; set; }
}
