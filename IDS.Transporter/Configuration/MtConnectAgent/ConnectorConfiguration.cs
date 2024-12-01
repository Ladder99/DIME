namespace IDS.Transporter.Configuration.MtConnectAgent;

public sealed class ConnectorConfiguration : Configuration.ConnectorConfiguration<Configuration.ConnectorItem>
{
    public int Port { get; set; }
    public string DeviceUuid { get; set; }
    public string DeviceId { get; set; }
    public string DeviceName { get; set; }
}