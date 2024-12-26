using Orleans.Concurrency;

namespace Orleans.Pipeline.Shared;

/// <summary>
/// The pipe grain interface for server side access
/// </summary>
/// <typeparam name="TToServer"></typeparam>
/// <typeparam name="TFromServer"></typeparam>
[Alias("Orleans.Pipeline.Shared.IOrleansPipeGrain`2")]
public interface IOrleansPipeGrain<TToServer, TFromServer> : IGrainWithStringKey
{
    /// <summary>
    /// Writes an item to the pipe towards the server
    /// </summary>
    /// <param name="item"></param>
    /// <returns></returns>
    [Alias("Write")]
    [Alias("Write"), AlwaysInterleave]
    Task Write(PipeTransferItem<TToServer> item);

    /// <summary>
    /// Subscribes to the pipe observer
    /// </summary>
    /// <param name="observer"></param>
    /// <returns></returns>
    [Alias("Subscribe")]
    Task Subscribe(IOrleansPipeObserver<TFromServer> observer);

    /// <summary>
    /// Unsubscribes from the pipe observer
    /// </summary>
    /// <param name="observer"></param>
    /// <returns></returns>
    [Alias("Unsubscribe")]
    Task Unsubscribe(IOrleansPipeObserver<TFromServer> observer);
}