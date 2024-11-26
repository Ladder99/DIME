using IDS.Transporter.Connectors;
using IDS.Transporter.Configuration.MtConnect;

namespace IDS.Transporter.Configurator.MtConnect;

public static class Sink
{
    public static IConnector Create(Dictionary<object, object> section, Disruptor.Dsl.Disruptor<MessageBoxMessage> disruptor)
    {
        ConnectorConfiguration config = new();
        config.ConnectorType = section.ContainsKey("connector") ? Convert.ToString(section["connector"]) : "MQTT";
        config.Direction = Configuration.ConnectorDirectionEnum.Sink;
        config.Enabled = section.ContainsKey("enabled") ? Convert.ToBoolean(section["enabled"]) : true;
        config.ScanIntervalMs = section.ContainsKey("scan_interval") ? Convert.ToInt32(section["scan_interval"]) : 1000;
        config.Name = section.ContainsKey("name") ? Convert.ToString(section["name"]) : Guid.NewGuid().ToString();
        config.Port = section.ContainsKey("port") ? Convert.ToInt32(section["port"]) : 1883;
        config.DeviceKey = section.ContainsKey("device_key") ? Convert.ToString(section["device_key"]) : null;
        config.HeartbeatMs = section.ContainsKey("heartbeat_ms") ? Convert.ToInt32(section["heartbeat_ms"]) : 10000;
        config.FilterDuplicates = section.ContainsKey("filter_duplicates") ? Convert.ToBoolean(section["filter_duplicates"]) : true;
        var connector = new Connectors.MtConnect.Sink(config, disruptor);

        return connector;
    }
}