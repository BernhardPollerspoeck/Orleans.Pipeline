using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Orleans.Pipeline.Client;

internal class OrleansPipelineClient(
    IClusterClient clusterClient,
    IServiceProvider serviceProvider)
    : IOrleansPipelineClient
{
    private readonly Dictionary<string, List<object>> _pipes = [];

    public IOrleansPipe<TToServer, TFromServer> GetPipe<TToServer, TFromServer>(string pipeKey)
    {
        // Get the list of pipes for the given key
        if (!_pipes.TryGetValue(pipeKey, out List<object>? pipesForKey))
        {
            pipesForKey = ([CreatePipe<TToServer, TFromServer>(pipeKey)]);
            _pipes[pipeKey] = pipesForKey;
        }

        // Get the pipe with matching types
        var resultPipe = pipesForKey.Cast<IOrleansPipe<TToServer, TFromServer>>().FirstOrDefault();
        if (resultPipe == null)
        {
            resultPipe = CreatePipe<TToServer, TFromServer>(pipeKey);
            pipesForKey.Add(resultPipe);
        }

        return resultPipe;
    }

    private OrleansPipe<TToServer, TFromServer> CreatePipe<TToServer, TFromServer>(string id)
    {
        return new OrleansPipe<TToServer, TFromServer>(
            clusterClient, 
            serviceProvider.GetRequiredService<ILogger<OrleansPipe<TToServer, TFromServer>>>(), 
            id);
    }
}
