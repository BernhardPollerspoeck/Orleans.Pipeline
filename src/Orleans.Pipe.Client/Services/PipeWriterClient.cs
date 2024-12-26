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

            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    var status = await pipe.TryWriteAsync(message, stoppingToken);
                    if (status == OrleansPipeStatus.Healthy)
                    {
                        logger.LogInformation("Successfully wrote to pipe.");
                        break;
                    }

                    if (status == OrleansPipeStatus.Broken)
                    {
                        logger.LogError("Pipe is broken. Giving up on writing.");
                        return; 
                    }

                    logger.LogWarning("Pipe is recovering. Retrying...");
                    await Task.Delay(100, stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while attempting to write to the pipe.");
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }

            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }
    }

}

