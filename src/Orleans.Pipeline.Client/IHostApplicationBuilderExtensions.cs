using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Orleans.Pipeline.Client;

public static class IHostApplicationBuilderExtensions
{
    public static IHostApplicationBuilder AddOrleansPipeline(
        this IHostApplicationBuilder services)
    {
        services.Services.AddSingleton<IOrleansPipelineClient, OrleansPipelineClient>();


        return services;
    }
}