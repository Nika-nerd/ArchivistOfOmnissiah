namespace Archivist.Services;

public class LoreBackgroundWorker : BackgroundService
{
    private readonly IBackgroundTaskQueue _taskQueue;
    private readonly ILogger<LoreBackgroundWorker> _logger;

    public LoreBackgroundWorker(IBackgroundTaskQueue taskQueue, ILogger<LoreBackgroundWorker> logger)
    {
        _taskQueue = taskQueue;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Machine Spirit Worker is starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            var workItem = await _taskQueue.DequeueAsync(stoppingToken);

            try
            {
                await workItem(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred executing work item.");
            }
        }
    }
}