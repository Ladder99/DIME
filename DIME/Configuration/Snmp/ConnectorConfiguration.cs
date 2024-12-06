namespace DIME.Configuration.Snmp;

public sealed class ConnectorConfiguration : Configuration.ConnectorConfiguration<ConnectorItem>
{
    public string Address { get; set; }
    public int Port { get; set; }
    public string Community { get; set; }
    public int TimeoutMs { get; set; }
}