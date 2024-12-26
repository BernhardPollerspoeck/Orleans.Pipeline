namespace Orleans.Pipeline.Client;

/// <summary>  
/// A client that provider all the pipes for a given client  
/// </summary>  
public interface IOrleansPipelineClient
{
    /// <summary>
    /// Get a pipe for a given key with the specified types
    /// </summary>
    /// <typeparam name="TToServer"></typeparam>
    /// <typeparam name="TFromServer"></typeparam>
    /// <param name="pipeKey"></param>
    /// <returns></returns>
    IOrleansPipe<TToServer, TFromServer> GetPipe<TToServer, TFromServer>(string pipeKey);
}
