using IDS.Transporter.Configuration;
using Newtonsoft.Json;

namespace IDS.Transporter.Connectors;

public class MessageBoxMessage
{
    public string Path { get; set; }
    public object Data { get; set; }
    public long Timestamp { get; set; }
    [JsonIgnore]
    public ConnectorItem ConnectorItemRef { get; set; }
}