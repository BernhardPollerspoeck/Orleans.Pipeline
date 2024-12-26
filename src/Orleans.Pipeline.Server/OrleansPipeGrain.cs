using Microsoft.Extensions.Logging;
using Orleans.Pipeline.Shared;
using Orleans.Utilities;

namespace Orleans.Pipeline.Server;

public abstract class OrleansPipeGrain<TToServer, TFromServer>(
    ILogger logger)
    : Grain,
    IOrleansPipeGrain<TToServer, TFromServer>
{
    #region fields
    private readonly ObserverManager<IOrleansPipeObserver<TFromServer>> _observers
        = new(TimeSpan.FromMinutes(2), logger);
    #endregion

    #region abstract
    protected abstract Task OnData(TToServer data);
    #endregion

    #region IOrleansPipeObserver<TFromServer>
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
    #endregion

    #region IOrleansPipeGrain<TToServer, TFromServer>
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
        return Task.CompletedTask;
    }
    #endregion
}
