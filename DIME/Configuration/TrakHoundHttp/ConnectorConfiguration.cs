namespace DIME.Configuration.TrakHoundHttp;

public sealed class ConnectorConfiguration : Configuration.ConnectorConfiguration<ConnectorItem>
{
    public string Address { get; set; }
    public int Port { get; set; }
    public bool UseSsl { get; set; }
    public string HostPath { get; set; }
    public string BasePath { get; set; }
    public string Router { get; set; }
}