using System.Threading.Channels;

namespace Orleans.Pipeline.Client;



/// <summary>
/// The actual workable pipe
/// </summary>
/// <typeparam name="TToServer"></typeparam>
/// <typeparam name="TFromServer"></typeparam>
public interface IOrleansPipe<TToServer, TFromServer>
{
    /// <summary>
    /// The reader for incomming data
    /// </summary>
    ChannelReader<TFromServer> Reader { get; }
    /// <summary>
    /// The writer for outgoing data
    /// </summary>
    ChannelWriter<TToServer> Writer { get; }

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
}

