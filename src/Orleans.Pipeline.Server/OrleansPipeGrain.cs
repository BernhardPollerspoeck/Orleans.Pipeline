using Microsoft.Extensions.Logging;
using Orleans.Pipeline.Shared;
using Orleans.Utilities;

namespace Orleans.Pipeline.Server;

[KeepAlive]
public abstract class OrleansPipeGrain<TToServer, TFromServer>(ILogger logger)
    : Grain, IOrleansPipeGrain<TToServer, TFromServer>
{
    private static readonly TimeSpan HeartbeatPeriod = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan HeartbeatTimeout = TimeSpan.FromSeconds(2);

    private IGrainTimer? _heartbeatTimer;
    private CancellationTokenSource _heartbeatCts = new();

    private readonly ObserverManager<IOrleansPipeObserver<TFromServer>> _manager = new(TimeSpan.FromMinutes(2), logger);
   
    protected abstract Task OnData(TToServer data);

    protected Task Notify(TFromServer data)
    {
        return _manager.Notify(observer
            => observer.ReceiveMessage(new PipeTransferItem<TFromServer>
            {
                Mode = TransferMode.Data,
                Item = data,
                Metadata = this.GetPrimaryKeyString(),
            }));
    }

    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        _heartbeatTimer = this.RegisterGrainTimer(PeriodicallySendHeartbeat, new GrainTimerCreationOptions()
        {
            Interleave = true, // allow interleaving so clients can still write data while heartbeat is running.
            DueTime = HeartbeatPeriod,
            Period = HeartbeatPeriod
        });

        return Task.CompletedTask;
    }

    public override Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        _heartbeatTimer?.Dispose();
        _heartbeatTimer = null;

        return Task.CompletedTask;
    }

    public async Task Write(PipeTransferItem<TToServer> item)
    {
        logger.LogInformation("Received transfer with mode {mode}", item.Mode);

        var task = item switch
        {
            { Mode: TransferMode.Data, Item: not null } => OnData(item.Item),
            _ => Task.CompletedTask
        };

        await task; // await the task so it can interleave with the heartbeat mechanism
    }

    public Task Subscribe(IOrleansPipeObserver<TFromServer> observer)
    {
        _manager.Subscribe(observer, observer);
        return Task.CompletedTask;
    }

    public Task Unsubscribe(IOrleansPipeObserver<TFromServer> observer)
    {
        _manager.Unsubscribe(observer);
        return Task.CompletedTask;
    }

    private async Task PeriodicallySendHeartbeat()
    {
        if (_manager.Count == 0)
        {
            _heartbeatTimer?.Dispose();
            _heartbeatTimer = null;

            DeactivateOnIdle();

            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("PipeGrain with Id = {GrainId} is deactivating because " +
                    "there are no observers attached anymore.", this.GetPrimaryKeyString());
            }

            return;
        }

        await _manager.Notify(_ => Task.CompletedTask); // clear any potentially defunct once, but dont notify serially.

        var healthyObservers = _manager.Observers.Select(x => x.Value); // no need to snapshot, its already one.

        var key = this.GetPrimaryKeyString();
        var hbTasks = new List<Task<IOrleansPipeObserver<TFromServer>>>();

        foreach (var observer in healthyObservers)
        {
            hbTasks.Add(SendHeartbeat(observer, key));
        }

        _heartbeatCts.CancelAfter(HeartbeatTimeout);
        await foreach (var hbTask in Task.WhenEach(hbTasks).WithCancellation(_heartbeatCts.Token))
        {
            var observer = await hbTask;
            if (hbTask.IsFaulted)
            {
                _manager.Unsubscribe(observer);
                if (logger.IsEnabled(LogLevel.Information))
                {
                    logger.LogInformation("Failed to contact observer, therefor i have removed it from my list");
                }
            }
        }

        if (!_heartbeatCts.TryReset())
        {
            _heartbeatCts.Dispose();
            _heartbeatCts = new();
        }

        if (logger.IsEnabled(LogLevel.Trace) || true)
        {
            logger.LogTrace("Heartbeat round finished for grain = {Key}", key);
        }

        static async Task<IOrleansPipeObserver<TFromServer>> SendHeartbeat(
            IOrleansPipeObserver<TFromServer> observer, string metadata)
        {
            TaskCompletionSource<IOrleansPipeObserver<TFromServer>> tcs = new();

            try
            {
                await observer.ReceiveMessage(new PipeTransferItem<TFromServer>()
                {
                    Item = default,
                    Metadata = metadata,
                    Mode = TransferMode.Heartbeat
                });

                tcs.SetResult(observer);
            }
            catch (Exception ex)
            { 
                tcs.SetException(ex);
            }

            return await tcs.Task;
        }
    }
}
