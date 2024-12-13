namespace DIME.Configuration.OpcUa;

public sealed class ConnectorConfiguration : Configuration.ConnectorConfiguration<ConnectorItem>
{
    public string Address { get; set; }
    public int Port { get; set; }
    public int TimeoutMs { get; set; }
    public bool UseAnonymousUser { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }
}