namespace ProductManagementSystem.Api.Entities.ConfigurationModels;

public class OtpSettingsConfig
{
    public int OtpLength { get; set; }
    public double ExpiresAfterMinutes { get; set; }
    public double DeleteAfterMinutes { get; set; }
}
