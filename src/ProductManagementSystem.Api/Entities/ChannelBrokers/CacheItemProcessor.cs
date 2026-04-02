namespace ProductManagementSystem.Api.Entities.ChannelBrokers;

public class CacheItemProcessor<T> where T : class
{
    public T Data { get; set; }
    public Guid ProcessorId { get; set; }

    public CacheItemProcessor(T data)
    {
        Data = data;
        ProcessorId = Guid.NewGuid();
    }
}
