namespace DIME.Configuration;

public abstract class DatabaseConnectorConfiguration<TItem> : ConnectorConfiguration<TItem>
    where TItem: ConnectorItem
{
    public string ConnectionString { get; set; }
    public string CommandText { get; set; }
}