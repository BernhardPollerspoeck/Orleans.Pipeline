using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Orleans.Pipe.Contract;

namespace Orleans.Pipe.Client.Services;

internal class PipeWriterClient(
    IOrleansPipelineClient pipelineClient,
    ILogger<PipeWriterClient> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();

        var pipe = pipelineClient.GetPipe<string, string>("TestPipe 1");
        while (!stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation("Sending {input}", "Hello, World!");
            await pipe.Writer.WriteAsync($"Current time is {DateTime.Now.TimeOfDay}", stoppingToken);
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}

