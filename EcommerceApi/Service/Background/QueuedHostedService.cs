using Microsoft.Extensions.Hosting;

namespace EcommerceApi.Service.Background;

public class QueuedHostedService : BackgroundService
{
    private readonly IBackgroundTaskQueue _queue;
    private readonly IServiceProvider _rootProvider;
    private readonly ILogger<QueuedHostedService> _logger;

    public QueuedHostedService(IBackgroundTaskQueue queue, IServiceProvider rootProvider, ILogger<QueuedHostedService> logger)
    {
        _queue = queue;
        _rootProvider = rootProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var workItem = await _queue.DequeueAsync(stoppingToken);

            try
            {
                using var scope = _rootProvider.CreateScope();
                await workItem(scope.ServiceProvider, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Background task failed");
            }
        }
    }
}