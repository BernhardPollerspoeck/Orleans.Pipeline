namespace Orleans.Pipeline.Shared;

[Alias("Orleans.Pipeline.Shared.IOrleansPipeGrain`2")]
public interface IOrleansPipeGrain<TToServer, TFromServer> : IGrainWithStringKey
{
    [Alias("Write")]
    Task Write(PipeTransferItem<TToServer> item);

    [Alias("Subscribe")]
    Task Subscribe(IOrleansPipeObserver<TFromServer> observer);

    [Alias("Unsubscribe")]
    Task Unsubscribe(IOrleansPipeObserver<TFromServer> observer);

    [Alias("RenewSubscription")]
    Task RenewSubscription(
        IOrleansPipeObserver<TFromServer> oldObserver,
        IOrleansPipeObserver<TFromServer> newObserver);
}
