using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

namespace Orleans.Pipeline.Client;

internal class OrleansPipelineClient(
    IClusterClient clusterClient,
    ILoggerFactory loggerFactory,
    IServiceProvider serviceProvider)
    : IOrleansPipelineClient
{
    #region fields
    private readonly ConcurrentDictionary<string, List<object>> _pipes = [];
    #endregion

    #region IOrleansPipelineClient
    public IOrleansPipe<TToServer, TFromServer> GetPipe<TToServer, TFromServer>(string pipeKey)
    {
        // Get or add the list of pipes for the given key
        var pipesForKey = _pipes.GetOrAdd(pipeKey, _ => []);

        lock (pipesForKey) // Thread-safe access to the specific pipes
        {
            // Get the pipe with matching types
            var resultPipe = pipesForKey
                .Cast<IOrleansPipe<TToServer, TFromServer>>()
                .FirstOrDefault();

            if (resultPipe is null)
            {
                // Create a new pipe if none exists
                resultPipe = CreatePipe<TToServer, TFromServer>(pipeKey);
                pipesForKey.Add(resultPipe);
            }

            return resultPipe;
        }
    }
    #endregion

    #region factory 
    private OrleansPipe<TToServer, TFromServer> CreatePipe<TToServer, TFromServer>(string id)
    {
        return new OrleansPipe<TToServer, TFromServer>(
            clusterClient,
            loggerFactory.CreateLogger<OrleansPipe<TToServer, TFromServer>>(),
            id,
            serviceProvider.GetRequiredService<IOptions<OrleansPipeConfiguration>>());
    }
    #endregion
}