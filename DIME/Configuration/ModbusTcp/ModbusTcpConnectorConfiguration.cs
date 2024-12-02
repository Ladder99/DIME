namespace DIME.Configuration.ModbusTcp;

public sealed class ConnectorConfiguration : Configuration.ConnectorConfiguration<ConnectorItem>
{
    public string IpAddress { get; set; }
    public int Port { get; set; }
    public byte Slave { get; set; }
    public int TimeoutMs { get; set; }
}