using Microsoft.Extensions.Logging;
using Orleans.Pipeline.Server;

namespace Orleans.Pipe.Silo;

public class TestGrain(ILogger<TestGrain> logger)
    : OrleansPipeGrain<string, string>(logger)
{

    override protected Task OnData(string data)
    {
        logger.LogInformation("Received {data}", data);
        return Notify("I got your message!");
    }

}