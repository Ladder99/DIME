namespace DIME.Configuration.HttpServer;

public sealed class ConnectorConfiguration : Configuration.ConnectorConfiguration<ConnectorItem>
{
    public string Uri { get; set; }
}