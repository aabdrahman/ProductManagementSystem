namespace ProductManagementSystem.Api.Entities.ConfigurationModels;

public class UserOrderVerificationConfig
{
    public double ExpiresAfterInMinutes { get; set; }
    public double DeleteAfterInMinutes { get; set; }
}
