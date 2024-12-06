namespace DIME.Configuration.MtConnectAgent;

public sealed class ConnectorConfiguration : Configuration.ConnectorConfiguration<ConnectorItem>
{
    public string Address { get; set; }
    public int Port { get; set; }
    public string DeviceUuid { get; set; }
    public string DeviceId { get; set; }
    public string DeviceName { get; set; }
}