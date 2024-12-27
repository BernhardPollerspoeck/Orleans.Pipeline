namespace Orleans.Pipeline.Server;

public class OrleansPipeConfiguration
{
    public TimeSpan HeartbeatInterval { get; set; } = TimeSpan.FromSeconds(5);
    public TimeSpan HeartbeatTimeout { get; set; } = TimeSpan.FromSeconds(2);
    public TimeSpan ObserverExpiration { get; set; } = TimeSpan.FromMinutes(2);
}
