namespace DIME.Configuration.InfluxLp;

public sealed class ConnectorConfiguration : Configuration.ConnectorConfiguration<ConnectorItem>
{
    public string Address { get; set; }
    public string Token { get; set; }
    public string Organization { get; set; }
    public string BucketName { get; set; }
}