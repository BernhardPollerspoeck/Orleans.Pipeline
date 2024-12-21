using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Orleans.Pipe.Contract;

namespace Orleans.Pipe.Client;

internal class PipeReaderClient(
    IOrleansPipelineClient pipelineClient,
    ILogger<PipeReaderClient> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        //return;//TODO:
        await Task.Yield();

        var pipe = pipelineClient.GetPipe<string, string>("TestPipe 1");
        await foreach (var result in pipe.Reader.ReadAllAsync(stoppingToken))
        {
            logger.LogInformation("Received {result}", result);
        }
    }
}

