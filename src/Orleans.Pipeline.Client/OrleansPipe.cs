using Microsoft.Extensions.Logging;
using Orleans.Pipeline.Shared;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Channels;

namespace Orleans.Pipeline.Client;

internal class OrleansPipe<TToServer, TFromServer>(
    IClusterClient clusterClient,
    ILogger<OrleansPipe<TToServer, TFromServer>> logger,
    string id)
    : IOrleansPipe<TToServer, TFromServer>
    , IOrleansPipeObserver<TFromServer>
{
    private readonly Channel<TToServer> _writer = Channel.CreateUnbounded<TToServer>();
    private readonly Channel<TFromServer> _reader = Channel.CreateUnbounded<TFromServer>();

    [AllowNull] private IOrleansPipeGrain<TToServer, TFromServer> _grain;
    [AllowNull] private IOrleansPipeObserver<TFromServer> _observer;

    private Task? _writerTask;
    private Task? _observerTask;
    private readonly CancellationTokenSource _observerStoppingTokenSource = new();

    #region IOleansPipe<TToServer, TFromServer>
    public ChannelReader<TFromServer> Reader => _reader.Reader;
    public ChannelWriter<TToServer> Writer => _writer.Writer;

    public async Task Start(CancellationToken token)
    {
        //get grain
        _grain = clusterClient.GetGrain<IOrleansPipeGrain<TToServer, TFromServer>>(id);

        //create task for writer invocation
        _writerTask = RunWriter(token);

        //create observer for reader
        _observerTask = RunObserver(token);
        _observer = clusterClient.CreateObjectReference<IOrleansPipeObserver<TFromServer>>(this);
        await _grain.Subscribe(_observer);
    }

    public async Task Stop(CancellationToken token)
    {
        try
        {
            // Cancel any ongoing operations
            _writer.Writer.Complete();
            await (_writerTask ?? Task.CompletedTask).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);

            _reader.Writer.Complete();
            await (_observerTask ?? Task.CompletedTask).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);

            _observerStoppingTokenSource.Cancel();

            // Unsubscribe from the grain if initialized
            if (_grain is not null && _observer is not null)
            {
                await _grain.Unsubscribe(_observer);
                clusterClient.DeleteObjectReference<IOrleansPipeObserver<TFromServer>>(_observer);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred during Stopping");
        }
        finally
        {
            _observerStoppingTokenSource.Dispose();
        }
    }

    #endregion

    #region IOrleansPipeObserver<TFromServer>
    public async Task ReceiveMessage(PipeTransferItem<TFromServer> message)
    {
        if (message is { Mode: TransferMode.Data, Item: not null })
        {
            await _reader.Writer.WriteAsync(message.Item);
        }
    }
    #endregion

    private async Task RunWriter(CancellationToken cancellationToken)
    {
        try
        {
            await foreach (var result in _writer.Reader.ReadAllAsync(cancellationToken))
            {
                var writeData = new PipeTransferItem<TToServer>
                {
                    Mode = TransferMode.Data,
                    Item = result,
                    Metadata = _grain.GetPrimaryKeyString(),
                };
                await _grain.Write(writeData);
            }
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("Writer stopped");
        }
    }
    private async Task RunObserver(CancellationToken cancellationToken)
    {
        try
        {
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _observerStoppingTokenSource.Token);
            while (!linkedCts.Token.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(100), linkedCts.Token);
                logger.LogInformation("Renewing subscription");
                await _grain.Subscribe(_observer);
            }
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("Observer stopped");
        }
    }
}