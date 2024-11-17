namespace ConsoleApp2.Configuration;

public sealed class EthernetIpConnectorConfiguration : ConnectorConfiguration<EthernetIpConnectorItem>
{
    public int PlcType { get; set; }
    public string IpAddress { get; set; }
    public string Path { get; set; }
    public int Log { get; set; }
    public int Timeout { get; set; }
}