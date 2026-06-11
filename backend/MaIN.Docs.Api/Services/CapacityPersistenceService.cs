namespace MaIN.Docs.Api.Services;

// Final safety net: flushes any pending CapacityService state to S3 on graceful
// shutdown. Holds no background loop/timer — normal flushes are piggybacked onto
// request handling in CapacityService (see RecordTokenUsage/GetCurrentTier), so the
// app stays free to scale to zero between requests.
public class CapacityPersistenceService(CapacityService capacity, ILogger<CapacityPersistenceService> logger) : IHostedService
{
    public Task StartAsync(CancellationToken ct) => Task.CompletedTask;

    public async Task StopAsync(CancellationToken ct)
    {
        try
        {
            await capacity.FlushIfDirtyAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "[Capacity] Final flush on shutdown failed");
        }
    }
}
