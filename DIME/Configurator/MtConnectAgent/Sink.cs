using DIME.Connectors;
using DIME.Configuration.MtConnectAgent;

namespace DIME.Configurator.MtConnectAgent;

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
        config.Port = section.ContainsKey("port") ? Convert.ToInt32(section["port"]) : 5000;
        config.DeviceUuid = section.ContainsKey("device_uuid") ? Convert.ToString(section["device_uuid"]) : Guid.NewGuid().ToString();
        config.DeviceId = section.ContainsKey("device_id") ? Convert.ToString(section["device_id"]) : Guid.NewGuid().ToString();
        config.DeviceName = section.ContainsKey("device_name") ? Convert.ToString(section["device_name"]) : Guid.NewGuid().ToString();
        var connector = new Connectors.MtConnectAgent.Sink(config, disruptor);

        return connector;
    }
}