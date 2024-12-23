namespace Orleans.Pipeline.Shared;

[Alias("Orleans.Pipeline.Shared.IOrleansPipeObserver`1")]
public interface IOrleansPipeObserver<TFromServer> : IGrainObserver
{
    [Alias("ReceiveMessage")]
    Task ReceiveMessage(PipeTransferItem<TFromServer> message);
}