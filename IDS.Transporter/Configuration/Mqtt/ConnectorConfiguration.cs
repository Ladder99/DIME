namespace IDS.Transporter.Configuration.Mqtt;

public sealed class ConnectorConfiguration : Configuration.ConnectorConfiguration<ConnectorItem>
{
    public string IpAddress { get; set; }
    public int Port { get; set; }
    public bool CleanSession { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }
    
    public string BaseTopic { get; set; }
}