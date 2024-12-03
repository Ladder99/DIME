namespace DIME.Configuration.AscCpc;

public sealed class ConnectorConfiguration : Configuration.ConnectorConfiguration<ConnectorItem>
{
    public string IpAddress { get; set; }
    public int Port { get; set; }
}