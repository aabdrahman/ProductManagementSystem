namespace ProductManagementSystem.Shared.DataTransferObjects.RequestParameters;

public sealed class MetaData
{
    public int currentPage { get; set; }
    public int pageSize { get; set; }
    public int totalPages { get; set; }
    public int totalCount { get; set; }

    public bool HasPrevious => currentPage > 1;
    public bool HasNext => currentPage < totalPages;
}