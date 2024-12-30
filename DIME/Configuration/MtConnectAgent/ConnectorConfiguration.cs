namespace DIME.Configuration.MtConnectAgent;

public sealed class ConnectorConfiguration : Configuration.ConnectorConfiguration<ConnectorItem>
{
    public string Address { get; set; }
    public int Port { get; set; }
    public string Device { get; set; }
}