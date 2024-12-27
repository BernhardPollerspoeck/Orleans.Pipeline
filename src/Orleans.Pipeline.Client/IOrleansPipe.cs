namespace Orleans.Pipeline.Client;

/// <summary>
/// The actual workable pipe
/// </summary>
/// <typeparam name="TToServer"></typeparam>
/// <typeparam name="TFromServer"></typeparam>
public interface IOrleansPipe<TToServer, TFromServer>
{
    /// <summary>
    /// Notification when status of the pipe changes.
    /// </summary>
    /// <remarks>Ensure exceptions are caught at call site</remarks>
    event Action<OrleansPipeStatus> OnStatusChanged;

    /// <summary>
    /// Starts the pipe
    /// </summary>
    /// <param name="token"></param>
    /// <returns></returns>
    Task Start(CancellationToken token);

    /// <summary>
    /// Stops the pipe
    /// </summary>
    /// <param name="token"></param>
    /// <returns></returns>
    Task Stop(CancellationToken token);

    /// <summary>
    /// Try to write <paramref name="item"/>.
    /// </summary>
    /// <param name="item"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <remarks>Dont write to a pipe if it returns <see cref="OrleansPipeStatus.Broken"/></remarks>
    /// <exception cref="BrokenOrleansPipeException"/>
    Task<bool> TryWriteAsync(TToServer item, CancellationToken cancellationToken = default);

    /// <summary>
    /// Consume pipe items.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    IAsyncEnumerable<TFromServer> ReadAllAsync(CancellationToken cancellationToken = default);
}