using DIME.Connectors;
using DIME.Configuration.MtConnectShdr;

namespace DIME.Configurator.MtConnectShdr;

public static class Sink
{
    public static IConnector Create(Dictionary<object, object> section, Disruptor.Dsl.Disruptor<MessageBoxMessage> disruptor)
    {
        ConnectorConfiguration config = new();
        config.ConnectorType = section.ContainsKey("connector") ? Convert.ToString(section["connector"]) : "MTConnectSHDR";
        config.Direction = Configuration.ConnectorDirectionEnum.Sink;
        config.Enabled = section.ContainsKey("enabled") ? Convert.ToBoolean(section["enabled"]) : true;
        config.ScanIntervalMs = section.ContainsKey("scan_interval") ? Convert.ToInt32(section["scan_interval"]) : 1000;
        config.Name = section.ContainsKey("name") ? Convert.ToString(section["name"]) : Guid.NewGuid().ToString();
        config.Port = section.ContainsKey("port") ? Convert.ToInt32(section["port"]) : 7878;
        config.DeviceKey = section.ContainsKey("device_key") ? Convert.ToString(section["device_key"]) : null;
        config.HeartbeatMs = section.ContainsKey("heartbeat_interval") ? Convert.ToInt32(section["heartbeat_interval"]) : 10000;
        config.FilterDuplicates = section.ContainsKey("filter_duplicates") ? Convert.ToBoolean(section["filter_duplicates"]) : true;
        var connector = new Connectors.MtConnectShdr.Sink(config, disruptor);

        return connector;
    }
}