namespace ConsoleApp2.Configuration;

public sealed class ModbusTcpConnectorConfiguration : ConnectorConfiguration<ModbusTcpConnectorItem>
{
    public string IpAddress { get; set; }
    public int Port { get; set; }
    public byte Slave { get; set; }
    public int Timeout { get; set; }
}