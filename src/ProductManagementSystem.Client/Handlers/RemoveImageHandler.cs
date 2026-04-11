namespace ProductManagementSystem.Client.Handlers;

public class RemoveImageHandler
{
    private readonly IWebHostEnvironment _webHostEnvironment;

    public RemoveImageHandler(IWebHostEnvironment webHostEnvironment)
    {
        _webHostEnvironment = webHostEnvironment;
    }

    public async Task<(bool isSuccessful, string responseMessage)> Handle(List<string> filesToRemove)
    {
        var filePath = Path.Combine(_webHostEnvironment.WebRootPath, "Images");

        int resultToProcess = filesToRemove.Count;
        int successCount = 0; int failureCount = 0;

        foreach (var file in filesToRemove)
        {

            try
            {
                File.Delete(Path.Combine(filePath, file));
                successCount++;
            }
            catch (Exception ex)
            {
                failureCount++;
            }

        }

        return (resultToProcess == successCount, "Selected files removed successfully.");
    }
}
