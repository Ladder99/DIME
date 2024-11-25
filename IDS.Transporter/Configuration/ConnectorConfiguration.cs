namespace IDS.Transporter.Configuration;

public abstract class ConnectorConfiguration<TItem> where TItem: ConnectorItem
{
    public ConnectorDirectionEnum Direction { get; set; }
    public string Name { get; set; }
    public bool Enabled { get; set; }
    public string ConnectorType { get; set; }
    public int ScanInterval { get; set; }
    public List<TItem> Items { get; set; }
}