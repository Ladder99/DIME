namespace ConsoleApp2.Configuration.Mqtt;

public sealed class ConnectorConfiguration : ConnectorConfiguration<ConnectorItem>
{
    public string IpAddress { get; set; }
    public int Port { get; set; }
    public bool CleanSession { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }
}