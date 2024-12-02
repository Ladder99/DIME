namespace DIME.Configuration.HaasShdr;

public sealed class ConnectorConfiguration : Configuration.ConnectorConfiguration<ConnectorItem>
{
    public string IpAddress { get; set; }
    public int Port { get; set; }
    public int TimeoutMs { get; set; }
    public int HeartbeatMs { get; set; }
    public int RetryMs { get; set; }
}