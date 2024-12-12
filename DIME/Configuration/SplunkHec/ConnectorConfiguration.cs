namespace DIME.Configuration.SplunkHec;

public sealed class ConnectorConfiguration : Configuration.ConnectorConfiguration<ConnectorItem>
{
    public string Address { get; set; }
    public int Port { get; set; }
    public bool UseSsl { get; set; }
    public string Token { get; set; }
    public string EventOrMetric { get; set; }
    public string Source { get; set; }
    public string SourceType { get; set; }
}