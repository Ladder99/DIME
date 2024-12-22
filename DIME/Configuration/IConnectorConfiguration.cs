namespace DIME.Configuration;

public interface IConnectorConfiguration
{
    public ConnectorDirectionEnum Direction { get; }
    public string Name { get; }
    public bool Enabled { get; }
    public string ConnectorType { get; }
    public int ScanIntervalMs { get; }
    public string InitScript { get; }
    public string DeinitScript { get; }
    public string LoopEnterScript { get; }
    public string LoopExitScript { get; }
    public List<string> ExcludeFilter { get; }
    public List<string> IncludeFilter { get; }
    public Dictionary<object, object> SinkMeta { get; }
    public bool StripPathPrefix { get; }
    public bool UseSinkTransform { get; }
}