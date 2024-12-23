namespace DIME.Configuration.OpcDa;

public sealed class ConnectorConfiguration : Configuration.ConnectorConfiguration<ConnectorItem>
{
    public string Address { get; set; }
}