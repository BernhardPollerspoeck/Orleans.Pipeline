using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Orleans.Pipe.Contract;
using System.Threading.Channels;

namespace Orleans.Pipe.Client;

public static class IHostApplicationBuilderExtensions
{
    public static IHostApplicationBuilder AddOrleansPipeline(
        this IHostApplicationBuilder services)
    {


        return services;
    }
}




//internal class OrleansPipe<TInput, TResult> : IOrleansPipe<TInput, TResult>
//{
//    private Channel<TResult> _reader;
//    public ChannelReader<TResult> Reader => _reader.Reader;
//    public Channel<TResult> Writer { get; }
//    private readonly IClusterClient clusterClient;
//    private readonly int id;
//    private ITestGrain grain;
//    public OrleansPipe(IClusterClient clusterClient, int id)
//    {
//        this.clusterClient = clusterClient;
//        this.id = id;
//        Reader = Channel.CreateUnbounded<TResult>();
//        Writer = Channel.CreateUnbounded<TResult>();
//    }
//    public async Task Start()
//    {
//        grain = clusterClient.GetGrain<ITestGrain>(id);
//        await Task.Yield();
//        await Task.Run(async () =>
//        {
//            await foreach (var input in Writer.Reader.ReadAllAsync())
//            {
//                var result = await grain.Reverse(input.ToString());
//                await Reader.Writer.WriteAsync(result);
//            }
//        });
//    }
//    public Task Stop()
//    {
//        Reader.Writer.Complete();
//        Writer.Writer.Complete();
//        return Task.CompletedTask;
//    }
//}


