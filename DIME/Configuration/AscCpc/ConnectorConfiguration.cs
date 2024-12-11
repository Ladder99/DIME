namespace DIME.Configuration.AscCpc;

public sealed class ConnectorConfiguration : Configuration.ConnectorConfiguration<ConnectorItem>
{
    public string Address { get; set; }
    public int Port { get; set; }
    public bool BypassPing { get; set; }
}