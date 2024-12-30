namespace DIME.Configuration.MongoDb;

public sealed class ConnectorConfiguration : Configuration.ConnectorConfiguration<ConnectorItem>
{
    public string Address { get; set; }
    public string Database { get; set; }
    public string Collection { get; set; }
}