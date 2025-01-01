namespace DIME.Configuration.Mqtt;

public sealed class ConnectorConfiguration : Configuration.ConnectorConfiguration<ConnectorItem>
{
    public string Address { get; set; }
    public int Port { get; set; }
    public bool CleanSession { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }
    public string BaseTopic { get; set; }
    public int QoS { get; set; }
    public bool RetainPublish { get; set; }
    public bool UseTurbo { get; set; }
}