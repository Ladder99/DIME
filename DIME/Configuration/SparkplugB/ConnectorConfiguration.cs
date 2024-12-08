namespace DIME.Configuration.SparkplugB;

public sealed class ConnectorConfiguration : Configuration.ConnectorConfiguration<ConnectorItem>
{
    public string Address { get; set; }
    public int Port { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }
    public string HostId { get; set; }
    public string GroupId { get; set; }
    public string NodeId { get; set; }
    public string DeviceId { get; set; }
    public int ReconnectIntervalMs { get; set; }
    public int BirthDelayMs { get; set; }
}