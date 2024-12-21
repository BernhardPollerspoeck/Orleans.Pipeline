using Microsoft.Extensions.Hosting;
using Orleans.Pipe.Contract;

namespace Orleans.Pipe.Client.Services;

internal class PipeShutdownClient(
    IOrleansPipelineClient pipelineClient) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
    public Task StopAsync(CancellationToken cancellationToken)
    {
        var pipe = pipelineClient.GetPipe<string, string>("TestPipe 1");
        return pipe.Stop(cancellationToken);
    }
}

