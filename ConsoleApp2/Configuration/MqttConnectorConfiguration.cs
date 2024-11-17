namespace ConsoleApp2.Configuration;

public sealed class MqttConnectorConfiguration : ConnectorConfiguration<MqttConnectorItem>
{
    public string IpAddress { get; set; }
    public int Port { get; set; }
    public bool CleanSession { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }
}