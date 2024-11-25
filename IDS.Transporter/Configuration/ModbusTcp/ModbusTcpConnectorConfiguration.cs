namespace IDS.Transporter.Configuration.ModbusTcp;

public sealed class ConnectorConfiguration : Configuration.ConnectorConfiguration<ConnectorItem>
{
    public string IpAddress { get; set; }
    public int Port { get; set; }
    public byte Slave { get; set; }
    public int Timeout { get; set; }
}