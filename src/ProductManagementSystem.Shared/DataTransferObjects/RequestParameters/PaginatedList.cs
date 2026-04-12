namespace ProductManagementSystem.Shared.DataTransferObjects.RequestParameters;

public class PaginatedList<T> : List<T>
{
    public MetaData metaData { get; set; }

    public PaginatedList(List<T> items, int pageSize, int pageNumber, int count)
    {
        metaData = new MetaData()
        {
            pageSize = pageSize,
            currentPage = pageNumber,
            totalCount = count,
            totalPages = (int)Math.Ceiling((double)count/pageSize)
        };
        AddRange(items);
    }

    public static PaginatedList<T> ToPagedList(IEnumerable<T> items, int pageSize, int pageNumber, int count)
    {
        return new PaginatedList<T>(items.ToList(), pageSize, pageNumber, count);
    }
}
