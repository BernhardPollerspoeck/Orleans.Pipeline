namespace Orleans.Pipeline.Shared;

/// <summary>
/// The observer for the pipe. This Is not directly used by the library users
/// </summary>
/// <typeparam name="TFromServer"></typeparam>
[Alias("Orleans.Pipeline.Shared.IOrleansPipeObserver`1")]
public interface IOrleansPipeObserver<TFromServer> : IGrainObserver
{
    /// <summary>
    /// Receives a message from the pipe
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    [Alias("ReceiveMessage")]
    Task ReceiveMessage(PipeTransferItem<TFromServer> message);
}