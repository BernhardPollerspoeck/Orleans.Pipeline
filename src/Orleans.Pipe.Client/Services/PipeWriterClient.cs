using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Orleans.Pipeline.Client;

namespace Orleans.Pipe.Client.Services;

internal class PipeWriterClient(
    IOrleansPipelineClient pipelineClient,
    ILogger<PipeWriterClient> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var pipe = pipelineClient.GetPipe<string, string>("TestPipe 1");
        while (!stoppingToken.IsCancellationRequested)
        {
            var message = $"Current time is {DateTime.Now.TimeOfDay}";
            logger.LogInformation("Sending {input}", message);
            await pipe.Writer.WriteAsync(message, stoppingToken);
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

