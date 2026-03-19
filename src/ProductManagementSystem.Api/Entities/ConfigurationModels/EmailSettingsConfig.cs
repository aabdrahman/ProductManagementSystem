namespace ProductManagementSystem.Api.Entities.ConfigurationModels;

public class EmailSettingsConfig
{
    public int Port { get; set; }
    public string Host { get; set; }
    public string DefaultFrom { get; set; }
    public int TotalBatchSize { get; set; }
}
