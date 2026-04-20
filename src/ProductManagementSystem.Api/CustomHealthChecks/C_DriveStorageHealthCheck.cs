using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ProductManagementSystem.Api.CustomHealthChecks;

public class C_DriveStorageHealthCheck : IHealthCheck
{
    private readonly long _minimumAvailableSizeInBytes;

    public C_DriveStorageHealthCheck(int minimumAvailableSizeInMb = 500)
    {
        _minimumAvailableSizeInBytes = minimumAvailableSizeInMb * 1024 * 1024;
    }

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {

        try
        {
            var drives = DriveInfo.GetDrives();

            var drive = DriveInfo.GetDrives().FirstOrDefault(x => x.IsReady && x.Name.StartsWith("C"));

            if(drive is null)
            {
                return Task.FromResult(HealthCheckResult.Unhealthy("C Drive is not available for check."));
            }

            var freeBytes = drive.AvailableFreeSpace;
            var freeMb = freeBytes / 1024 * 1024;

            var data = new Dictionary<string, object>()
            {
                ["free_mb"] = freeMb,
                ["total_mb"] = drive.TotalSize / 1024 * 1024
            };

            if(freeBytes < _minimumAvailableSizeInBytes)
            {
                return Task.FromResult(HealthCheckResult.Unhealthy($"Low disk space on C drive: {freeMb} MB remaining.", data: data));
            }

            if(freeBytes < _minimumAvailableSizeInBytes * 2)
            {
                return Task.FromResult(HealthCheckResult.Degraded($"Available space is growing on C drive. {freeMb} MB remaining.", data: data));
            }

            return Task.FromResult(HealthCheckResult.Healthy("C drive Disk is running optimally on enough space", data: data));
        }
        catch (Exception ex)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("An error occurred getting disk space from C drive root folder.", ex));
        }

        throw new NotImplementedException();
    }
}
