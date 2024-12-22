namespace DIME.Configuration;

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
    public string DeinitScript { get; set; }
    public string LoopEnterScript { get; set; }
    public string LoopExitScript { get; set; }
    public List<string> ExcludeFilter { get; set; }
    public List<string> IncludeFilter { get; set; }
    public Dictionary<object, object> SinkMeta { get; set; }
    public bool StripPathPrefix { get; set; }
    public bool UseSinkTransform { get; set; }
    public List<TItem> Items { get; set; }
}