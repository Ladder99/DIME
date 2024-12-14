using DIME.Connectors;
using DIME.Configuration.MtConnectAgent;

namespace DIME.Configurator.MtConnectAgent;

public static class Sink
{
    public static IConnector Create(Dictionary<object, object> section, Disruptor.Dsl.Disruptor<MessageBoxMessage> disruptor)
    {
        ConnectorConfiguration config = ConfigurationHelper<ConnectorConfiguration, ConnectorItem>
            .MakeConfig(section, "MTConnectAgent", Configuration.ConnectorDirectionEnum.Sink);

        config.Port = section.ContainsKey("port") ? Convert.ToInt32(section["port"]) : 5000;
        config.DeviceUuid = section.ContainsKey("device_uuid") ? Convert.ToString(section["device_uuid"]) : Guid.NewGuid().ToString();
        config.DeviceId = section.ContainsKey("device_id") ? Convert.ToString(section["device_id"]) : Guid.NewGuid().ToString();
        config.DeviceName = section.ContainsKey("device_name") ? Convert.ToString(section["device_name"]) : Guid.NewGuid().ToString();
        
        var connector = new Connectors.MtConnectAgent.Sink(config, disruptor);

        return connector;
    }
}