namespace Orleans.Pipeline.Client;

public class OrleansPipeConfiguration
{
    public int MaxReconnectionAttempts { get; set; } = 3;
    public TimeSpan ReconnectionDelay { get; set; } = TimeSpan.FromMilliseconds(100);
    public TimeSpan ExpectedHeartbeatInterval { get; set; } = TimeSpan.FromSeconds(5);
    public TimeSpan ExpectedHeartbeatIntervalGracePeriod { get; set; } = TimeSpan.FromMilliseconds(150);
}