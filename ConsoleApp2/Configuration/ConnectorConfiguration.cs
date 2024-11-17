namespace ConsoleApp2.Configuration;

public abstract class ConnectorConfiguration<T> where T: ConnectorItem
{
    public ConnectorDirection Direction { get; set; }
    public string Name { get; set; }
    public bool Enabled { get; set; }
    public string ConnectorType { get; set; }
    public int ScanInterval { get; set; }
    public List<T> Items { get; set; }
}