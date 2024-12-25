﻿using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace Orleans.Pipeline.Client;

internal class OrleansPipelineClient(
    IClusterClient clusterClient,
    ILoggerFactory loggerFactory)
    : IOrleansPipelineClient
{
    private readonly ConcurrentDictionary<string, List<object>> _pipes = [];

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

            if (resultPipe == null)
            {
                // Create a new pipe if none exists
                resultPipe = CreatePipe<TToServer, TFromServer>(pipeKey);
                pipesForKey.Add(resultPipe);
            }

            return resultPipe;
        }
    }

    private OrleansPipe<TToServer, TFromServer> CreatePipe<TToServer, TFromServer>(string id)
    {
        return new OrleansPipe<TToServer, TFromServer>(
            clusterClient,
            loggerFactory.CreateLogger<OrleansPipe<TToServer, TFromServer>>(),
            id);
    }
}