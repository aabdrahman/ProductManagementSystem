using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ProductManagementSystem.Api.CustomHealthChecks;

public class FileStoragePathHealthCheck : IHealthCheck
{
    private readonly IWebHostEnvironment _webHostEnvironment;
    public FileStoragePathHealthCheck(IWebHostEnvironment webHostEnvironment)
    {
        _webHostEnvironment = webHostEnvironment;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            Console.WriteLine($"Content Root Path: {_webHostEnvironment.ContentRootPath}");
            var fileStoragePath = Path.Combine(_webHostEnvironment.ContentRootPath, "StaticFiles");

            if(!Directory.Exists(fileStoragePath))
            {
                return HealthCheckResult.Degraded(fileStoragePath + " does not exist. Please ensure the directory is created and accessible.");
            }

            if(File.Exists(Path.Combine(fileStoragePath, "test.txt")))
            {
                File.Delete(Path.Combine(fileStoragePath, "test.txt"));
            }

            await File.WriteAllTextAsync(Path.Combine(fileStoragePath, "test.txt"), "This is a health check test file.", cancellationToken);

            return HealthCheckResult.Healthy($"File storage path is accessible: {fileStoragePath}");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Degraded("An error occurred while checking the file storage path.", ex);
        }

        throw new NotImplementedException();
    }
}