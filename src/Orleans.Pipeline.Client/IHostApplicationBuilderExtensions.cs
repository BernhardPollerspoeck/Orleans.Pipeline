using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Orleans.Pipeline.Client;

/// <summary>
/// Extensions for the IHostApplicationBuilder
/// </summary>
public static class IHostApplicationBuilderExtensions
{
    /// <summary>
    /// Add the Orleans pipeline to the host application builder
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IHostApplicationBuilder AddOrleansPipeline(
        this IHostApplicationBuilder services)
    {
        services.Services.AddSingleton<IOrleansPipelineClient, OrleansPipelineClient>();


        return services;
    }
}