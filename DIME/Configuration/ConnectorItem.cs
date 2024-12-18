namespace DIME.Configuration;

public class ConnectorItem
{
    public IConnectorConfiguration Configuration { get; set; }
    public string Name { get; set; }
    public bool Enabled { get; set; }
    public bool ReportByException { get; set; }
    public string Address { get; set; }
    public string Script { get; set; }
    public Dictionary<object, object> SinkMeta { get; set; }
}