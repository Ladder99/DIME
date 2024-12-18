namespace DIME.Configuration.HttpClient;

public sealed class ConnectorConfiguration : Configuration.ConnectorConfiguration<ConnectorItem>
{
    public string Uri { get; set; }
    public Dictionary<string, string> Headers { get; set; }
}