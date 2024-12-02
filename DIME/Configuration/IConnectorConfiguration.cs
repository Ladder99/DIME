namespace DIME.Configuration;

public interface IConnectorConfiguration
{
    public ConnectorDirectionEnum Direction { get; }
    public string Name { get; }
    public bool Enabled { get; }
    public string ConnectorType { get; }
    public int ScanIntervalMs { get; }
    public string InitScript { get; }
}