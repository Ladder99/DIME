namespace IDS.Transporter.Configuration.EthernetIp;

public sealed class ConnectorConfiguration : Configuration.ConnectorConfiguration<ConnectorItem>
{
    public int PlcType { get; set; }
    public string IpAddress { get; set; }
    public string Path { get; set; }
    public int Log { get; set; }
    public int TimeoutMs { get; set; }
}