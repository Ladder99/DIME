namespace DIME.Configuration.SplunkEhSdk;

public sealed class ConnectorConfiguration : Configuration.ConnectorConfiguration<ConnectorItem>
{
    public string Address { get; set; }
    public int Port { get; set; }
    public bool NumbersToMetrics { get; set; }
}