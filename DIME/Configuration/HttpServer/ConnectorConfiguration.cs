namespace DIME.Configuration.HttpServer;

public sealed class ConnectorConfiguration : Configuration.ConnectorConfiguration<Configuration.ConnectorItem>
{
    public string Uri { get; set; }
}