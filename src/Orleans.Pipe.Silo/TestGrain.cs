using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans.Pipeline.Server;

namespace Orleans.Pipe.Silo;

public class TestGrain(ILogger<TestGrain> logger, IOptions<OrleansPipeConfiguration> options)
    : OrleansPipeGrain<string, string>(logger, options)
{

    override protected Task OnData(string data)
    {
        logger.LogInformation("Received {data}", data);
        return Notify("I got your message!");
    }

}