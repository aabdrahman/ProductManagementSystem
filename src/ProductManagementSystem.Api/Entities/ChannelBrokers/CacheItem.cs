namespace ProductManagementSystem.Api.Entities.ChannelBrokers;

public class CacheItem
{
    public string Value { get; set; }
    public string Type { get; set; }
    public string Key { get; set; }
    public double ExpiresAfter { get; set; }
    public string? KeyIdentifier { get; set; }

    public CacheItem(string value, string type, string key, double expiresAfter, string? keyIdentifier = null)
    {
        Value = value;
        Type = type;
        Key = key;
        ExpiresAfter = expiresAfter;
        KeyIdentifier = keyIdentifier;
    }
}
