using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Orleans.Configuration;
using Orleans.Pipeline.Server;
using System.Net;

var builder = Host.CreateApplicationBuilder();


builder.Logging.ClearProviders();
builder.Logging.AddConsole();

builder.UseOrleans(siloBuilder =>
{
    siloBuilder.UseLocalhostClustering();
    siloBuilder.AddMemoryGrainStorage("store");

    siloBuilder.Configure<EndpointOptions>(options =>
    {
        // Port to use for silo-to-silo
        options.SiloPort = 11112;
        // Port to use for the gateway
        options.GatewayPort = 30000;
        // IP Address to advertise in the cluster
        options.AdvertisedIPAddress = IPAddress.Parse("127.0.0.1");
    });
    siloBuilder.Configure<SiloOptions>(o => o.SiloName = "Pipe");
    siloBuilder.Configure<ClusterOptions>(o =>
    {
        o.ClusterId = "pipe.cluster";
        o.ServiceId = "pipe";
    });
    siloBuilder.Configure<OrleansPipeConfiguration>(o =>
    {
        o.HeartbeatInterval = TimeSpan.FromSeconds(5);
        o.HeartbeatTimeout = TimeSpan.FromSeconds(2);
        o.ObserverExpiration = TimeSpan.FromMinutes(2);
    });
});


var host = builder.Build();

await host.RunAsync();



