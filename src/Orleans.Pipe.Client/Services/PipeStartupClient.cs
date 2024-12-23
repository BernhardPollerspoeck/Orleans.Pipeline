using Microsoft.Extensions.Hosting;
using Orleans.Pipeline.Client;

namespace Orleans.Pipe.Client.Services;

internal class PipeStartupClient(
    IOrleansPipelineClient pipelineClient) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        var pipe = pipelineClient.GetPipe<string, string>("TestPipe 1");
        return pipe.Start(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}

