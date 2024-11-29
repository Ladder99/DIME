namespace IDS.Transporter.Configuration;

public abstract class ConnectorConfiguration<TItem> : IConnectorConfiguration
    where TItem: ConnectorItem
{
    public ConnectorDirectionEnum Direction { get; set; }
    public string Name { get; set; }
    public bool Enabled { get; set; }
    public string ConnectorType { get; set; }
    public int ScanIntervalMs { get; set; }
    public bool ReportByException { get; set; }
    public bool ItemizedRead { get; set; }
    public string InitScript { get; set; }
    public List<TItem> Items { get; set; }
}