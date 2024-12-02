namespace IDS.Transporter.Configuration.HttpServer;

public sealed class ConnectorConfiguration : Configuration.ConnectorConfiguration<ConnectorItem>
{
    public string Uri { get; set; }
}