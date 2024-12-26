using Microsoft.Extensions.Logging;
using Orleans.Pipeline.Shared;
using Orleans.Utilities;

namespace Orleans.Pipeline.Server;

[KeepAlive]
public abstract class OrleansPipeGrain<TToServer, TFromServer>(ILogger logger)
    : Grain, IOrleansPipeGrain<TToServer, TFromServer>
{
    private static readonly TimeSpan TimerPeriod = TimeSpan.FromSeconds(30);
    private readonly ObserverManager<IOrleansPipeObserver<TFromServer>> _observers = new(TimeSpan.FromMinutes(2), logger);
    protected abstract Task OnData(TToServer data);

    protected Task Notify(TFromServer data)
    {
        return _observers.Notify(observer
            => observer.ReceiveMessage(new PipeTransferItem<TFromServer>
            {
                Mode = TransferMode.Data,
                Item = data,
                Metadata = this.GetPrimaryKeyString(),
            }));
    }

    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        this.RegisterGrainTimer(() =>
        {
            DeactivateIfNoMoreObservers();
            return Task.CompletedTask;

        }, new GrainTimerCreationOptions()
        {
            DueTime = TimerPeriod,
            Period = TimerPeriod
        });

        return Task.CompletedTask;
    }

    public Task Write(PipeTransferItem<TToServer> item)
    {
        logger.LogInformation("Received transfer with mode {mode}", item.Mode);
        return item switch
        {
            { Mode: TransferMode.Data, Item: not null } => OnData(item.Item),
            _ => Task.CompletedTask
        };
    }

    public Task Subscribe(IOrleansPipeObserver<TFromServer> observer)
    {
        _observers.Subscribe(observer, observer);
        return Task.CompletedTask;
    }

    public Task Unsubscribe(IOrleansPipeObserver<TFromServer> observer)
    {
        _observers.Unsubscribe(observer);
        DeactivateIfNoMoreObservers();

        return Task.CompletedTask;
    }

    private void DeactivateIfNoMoreObservers()
    {
        if (_observers.Count == 0)
        {
            DeactivateOnIdle();

            logger.LogInformation("PipeGrain with Id = {GrainId} is deactivating because " +
                "there are no observers attached anymore.", this.GetPrimaryKeyString());
        }
    }
}
