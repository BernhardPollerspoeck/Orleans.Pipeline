namespace Orleans.Pipeline.Client;

/// <summary>
/// The actual workable pipe
/// </summary>
/// <typeparam name="TToServer"></typeparam>
/// <typeparam name="TFromServer"></typeparam>
public interface IOrleansPipe<TToServer, TFromServer>
{
    Task Start(CancellationToken token);
    Task Stop(CancellationToken token);

    Task<OrleansPipeStatus> TryWriteAsync(TToServer item, CancellationToken cancellationToken = default);
    IAsyncEnumerable<TFromServer> ReadAllAsync(CancellationToken cancellationToken = default);
}