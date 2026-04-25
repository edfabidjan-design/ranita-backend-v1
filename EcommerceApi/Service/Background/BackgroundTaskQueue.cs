using System.Threading.Channels;

namespace EcommerceApi.Service.Background;

public interface IBackgroundTaskQueue
{
    void Enqueue(Func<IServiceProvider, CancellationToken, Task> workItem);
    ValueTask<Func<IServiceProvider, CancellationToken, Task>> DequeueAsync(CancellationToken cancellationToken);
}

public class BackgroundTaskQueue : IBackgroundTaskQueue
{
    private readonly Channel<Func<IServiceProvider, CancellationToken, Task>> _queue =
        Channel.CreateUnbounded<Func<IServiceProvider, CancellationToken, Task>>();

    public void Enqueue(Func<IServiceProvider, CancellationToken, Task> workItem)
        => _queue.Writer.TryWrite(workItem);

    public ValueTask<Func<IServiceProvider, CancellationToken, Task>> DequeueAsync(CancellationToken cancellationToken)
        => _queue.Reader.ReadAsync(cancellationToken);
}