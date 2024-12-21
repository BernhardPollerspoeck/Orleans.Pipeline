using System.IO.Pipelines;
using System.Threading.Channels;

namespace Orleans.Pipe.Contract;


public interface ITestGrain : IGrainWithIntegerKey
{
}

/// <summary>  
/// A client that provider all the pipes for a given client  
/// </summary>  
public interface IOrleansPipelineClient
{
    IOrleansPipe<TToServer, TFromServer> GetPipe<TToServer, TFromServer>(string pipeKey);
}



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
