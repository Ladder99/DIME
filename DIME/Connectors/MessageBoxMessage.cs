using DIME.Configuration;
using Newtonsoft.Json;

namespace DIME.Connectors;

public class MessageBoxMessage
{
    public string Path { get; set; }
    public object Data { get; set; }
    public long Timestamp { get; set; }
    [JsonIgnore]
    public ConnectorItem ConnectorItemRef { get; set; }
}