namespace ProductManagementSystem.Api.Entities.ConfigurationModels;

public class RemoveExpiredVerificationTokenBackgroundConfig
{
    public int RunAfterSeconds { get; set; }
    public double ExpiresAfterInMinutes { get; set; }
}
