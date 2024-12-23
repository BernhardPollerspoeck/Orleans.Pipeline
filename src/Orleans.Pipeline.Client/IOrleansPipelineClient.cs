namespace Orleans.Pipeline.Client;

/// <summary>  
/// A client that provider all the pipes for a given client  
/// </summary>  
public interface IOrleansPipelineClient
{
    IOrleansPipe<TToServer, TFromServer> GetPipe<TToServer, TFromServer>(string pipeKey);
}
