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
        pipe.OnStatusChanged += Pipe_OnStatusChanged;

        while (!stoppingToken.IsCancellationRequested)
        {
            var message = $"Current time is {DateTime.Now.TimeOfDay}";
            logger.LogInformation("Sending {input}", message);

            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    var success = await pipe.TryWriteAsync(message, stoppingToken);
                    if (success)
                    {
                        logger.LogInformation("Successfully wrote to pipe.");
                        break;
                    }

                    logger.LogWarning("Couldnt write to pipe. Retrying after a while.");
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

        pipe.OnStatusChanged -= Pipe_OnStatusChanged;
    }

    private void Pipe_OnStatusChanged(OrleansPipeStatus status) =>
        logger.LogInformation("Pipe status changed to {Stauts}", status.ToString());
}

