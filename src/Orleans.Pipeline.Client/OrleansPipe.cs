using Microsoft.Extensions.Logging;
using Orleans.Pipeline.Shared;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
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
    private long _lastHeartbeatTicks = 0;
    private OrleansPipeStatus _status = OrleansPipeStatus.Broken;
    private readonly SemaphoreSlim _reconnectionSemaphore = new(1, 1);
    private readonly CancellationTokenSource _observerStoppingTokenSource = new();
    private static readonly long ExpectedHeartbeatIntervalTicks = PipeConstants.HeartbeatInterval.Ticks;
    private static readonly TimeSpan ReconnectionDelay = TimeSpan.FromMilliseconds(100);
    private const int MaxReconnectionAttempts = 3;

    public event Action<OrleansPipeStatus>? OnStatusChanged;

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
        _status = OrleansPipeStatus.Healthy;
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
            logger.LogError(ex, "An error occurred during stopping.");
        }
        finally
        {
            _observerStoppingTokenSource.Dispose();
            UpdateStatus(OrleansPipeStatus.Broken);
        }
    }

    public async Task<bool> TryWriteAsync(TToServer item, CancellationToken cancellationToken)
    {
        if (_status == OrleansPipeStatus.Broken)
        {
            throw new BrokenOrleansPipeException("Cannot write to a broken pipe.");
        }

        if ((DateTime.UtcNow.Ticks - Interlocked.Read(ref _lastHeartbeatTicks)) < ExpectedHeartbeatIntervalTicks)
        {
            await _writer.Writer.WriteAsync(item, cancellationToken);
            return true;
        }

        if (!await _reconnectionSemaphore.WaitAsync(0, cancellationToken))
        {
            return false; // Another reconnection attempt is in progress.
        }

        try
        {
            UpdateStatus(OrleansPipeStatus.Recovering);

            for (int i = 0; i < MaxReconnectionAttempts; i++)
            {
                try 
                {
                    await _grain.Subscribe(_observer);
                }
                catch 
                {
                    await Task.Delay(ReconnectionDelay, cancellationToken);
                    continue;
                }

                Interlocked.Exchange(ref _lastHeartbeatTicks, DateTime.UtcNow.Ticks);
                await _writer.Writer.WriteAsync(item, cancellationToken);

                UpdateStatus(OrleansPipeStatus.Healthy);

                return true;
            }

            await Stop(CancellationToken.None); // This will stop the reader too, so clients can gracefully give up on this pipe.
            UpdateStatus(OrleansPipeStatus.Broken);

            return false;
        }
        finally
        {
            _reconnectionSemaphore.Release();
        }
    }

    public IAsyncEnumerable<TFromServer> ReadAllAsync(CancellationToken cancellationToken) =>
        _reader.Reader.ReadAllAsync(cancellationToken);

    public async Task ReceiveMessage(PipeTransferItem<TFromServer> message)
    {
        switch (message.Mode)
        {
            case TransferMode.Data:
                {
                    ArgumentNullException.ThrowIfNull(message.Item, "Item must no be null using data transfer mode");
                    await _reader.Writer.WriteAsync(message.Item);
                }
                break;
            case TransferMode.Heartbeat:
                Interlocked.Exchange(ref _lastHeartbeatTicks, DateTime.UtcNow.Ticks);
                break;
            default:
                throw new NotSupportedException($"The transfer mode '{message.Mode}' is not supported");
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void UpdateStatus(OrleansPipeStatus status)
    {
        Interlocked.Exchange(ref Unsafe.As<OrleansPipeStatus, int>(ref _status), (int)status);
        Interlocked.CompareExchange(ref OnStatusChanged, null, null)?.Invoke(status);
    }


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
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("Writer stopped");
            }
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
                if (logger.IsEnabled(LogLevel.Information))
                {
                    logger.LogInformation("Renewing subscription");
                }
                await _grain.Subscribe(_observer);
            }
        }
        catch (OperationCanceledException)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("Observer stopped");
            }
        }
    }
}