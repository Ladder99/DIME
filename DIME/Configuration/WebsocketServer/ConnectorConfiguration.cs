namespace DIME.Configuration.WebsocketServer;

public sealed class ConnectorConfiguration : Configuration.ConnectorConfiguration<ConnectorItem>
{
    public string Uri { get; set; }
}