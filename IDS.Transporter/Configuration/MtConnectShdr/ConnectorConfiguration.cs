namespace IDS.Transporter.Configuration.MtConnectShdr;

public sealed class ConnectorConfiguration : Configuration.ConnectorConfiguration<Configuration.ConnectorItem>
{
    public int Port { get; set; }
    public string DeviceKey { get; set; }
    public int HeartbeatMs { get; set; }
    public bool FilterDuplicates { get; set; }
}