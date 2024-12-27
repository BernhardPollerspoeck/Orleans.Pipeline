using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Orleans.Configuration;
using Orleans.Pipe.Client.Services;
using Orleans.Pipeline.Client;

var builder = Host.CreateApplicationBuilder();


builder.Logging.ClearProviders();
builder.Logging.AddConsole();


builder.AddOrleansPipeline(c =>
{
    c.MaxReconnectionAttempts = 3;
    c.ReconnectionDelay = TimeSpan.FromMilliseconds(100);
    c.ExpectedHeartbeatInterval = TimeSpan.FromSeconds(5);
    c.ExpectedHeartbeatIntervalGracePeriod = TimeSpan.FromMilliseconds(150);
});



builder.UseOrleansClient(clientBuilder =>
 {
     clientBuilder.Configure<ClusterOptions>(options =>
     {
         options.ClusterId = "pipe.cluster";
         options.ServiceId = "pipe";
     });
     clientBuilder.UseLocalhostClustering();

 });

builder.Services.AddHostedService<PipeStartupClient>();
builder.Services.AddHostedService<PipeReaderClient>();
builder.Services.AddHostedService<PipeWriterClient>();
builder.Services.AddHostedService<PipeShutdownClient>();

var host = builder.Build();

await host.RunAsync();