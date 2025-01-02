namespace DIME.Configuration.ActiveMq;

public sealed class ConnectorConfiguration : Configuration.ConnectorConfiguration<ConnectorItem>
{
    public string Address { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }
}