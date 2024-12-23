using Orleans.Pipeline.Shared;
using System.Threading.Channels;

namespace Orleans.Pipeline.Client;



/// <summary>
/// The actual workable pipe
/// </summary>
/// <typeparam name="TToServer"></typeparam>
/// <typeparam name="TFromServer"></typeparam>
public interface IOrleansPipe<TToServer, TFromServer>
{
    ChannelReader<TFromServer> Reader { get; }
    ChannelWriter<TToServer> Writer { get; }

    Task Start(CancellationToken token);
    Task Stop(CancellationToken token);
}

