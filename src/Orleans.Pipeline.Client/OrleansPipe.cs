using Microsoft.Extensions.Logging;
using Orleans.Pipeline.Shared;
using System;
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

    private IOrleansPipeGrain<TToServer, TFromServer>? _grain;

    private IOrleansPipeObserver<TFromServer>? _observer;
    private Task? _observerTask;
    private readonly CancellationTokenSource _observerStoppingTokenSource = new();

    private Task? _writerTask;
    private readonly CancellationTokenSource _writerStoppingTokenSource = new();

    #region IOleansPipe<TToServer, TFromServer>
    public ChannelReader<TFromServer> Reader => _reader.Reader;
    public ChannelWriter<TToServer> Writer => _writer.Writer;

    public Task Start(CancellationToken token)
    {
        //get grain
        _grain = clusterClient.GetGrain<IOrleansPipeGrain<TToServer, TFromServer>>(id);

        //create task for writer invocation
        _writerTask = RunWriter();

        //create observer for reader
        _observerTask = RunObserver();
        _observer = clusterClient.CreateObjectReference<IOrleansPipeObserver<TFromServer>>(this);
        return _grain.Subscribe(_observer);
    }
    public async Task Stop(CancellationToken token)
    {
        //finish writer channel
        _writer.Writer.Complete();
        _writerStoppingTokenSource.Cancel();
        if (_writerTask is not null)
        {
            await _writerTask;
        }

        //finish reader channel
        _reader.Writer.Complete();


        //dispose of grain
        _observerStoppingTokenSource.Cancel();
        if (_observerTask is not null)
        {
            await _observerTask;
        }
        if (this is { _grain: not null, _observer: not null })
        {
            await _grain!.Unsubscribe(_observer);
        }
    }
    #endregion

    #region IOrleansPipeObserver<TFromServer>
    public Task ReceiveMessage(PipeTransferItem<TFromServer> message)
    {
        if (message is { Mode: TransferMode.Data, Item: not null })
        {
            _reader.Writer.TryWrite(message.Item);
        }
        return Task.CompletedTask;
    }
    #endregion

    private async Task RunWriter()
    {
        try
        {
            await foreach (var result in _writer.Reader.ReadAllAsync(_writerStoppingTokenSource.Token))
            {
                var writeData = new PipeTransferItem<TToServer>
                {
                    Mode = TransferMode.Data,
                    Item = result,
                    Metadata = _grain.GetPrimaryKeyString(),
                };
                await _grain!.Write(writeData);
            }
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("Writer stopped");
        }
    }
    private async Task RunObserver()
    {
        try
        {
            while (!_observerStoppingTokenSource.Token.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(100), _observerStoppingTokenSource.Token);
                if (this is { _grain: not null, _observer: not null })
                {
                    logger.LogInformation("Renewing subscription");
                    var newObserver = clusterClient.CreateObjectReference<IOrleansPipeObserver<TFromServer>>(this);
                    await _grain.RenewSubscription(_observer, newObserver);
                    _observer = newObserver;
                }
            }
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("Observer stopped");
        }

    }
}
