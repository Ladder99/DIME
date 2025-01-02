using DIME.Connectors;
using DIME.Configuration.SparkplugB;

namespace DIME.Configurator.SparkplugB;

public static class Sink
{
    public static IConnector Create(Dictionary<object, object> section, Disruptor.Dsl.Disruptor<MessageBoxMessage> disruptor)
    {
        ConnectorConfiguration config = ConfigurationHelper<ConnectorConfiguration, ConnectorItem>
            .MakeConfig(section, "SparkplugB", Configuration.ConnectorDirectionEnum.Sink);

        config.Address = section.ContainsKey("address") ? Convert.ToString(section["address"]) : string.Empty;
        config.Port = section.ContainsKey("port") ? Convert.ToInt32(section["port"]) : 1883;
        config.Username = section.ContainsKey("username") ? Convert.ToString(section["username"]) : null;
        config.Password = section.ContainsKey("password") ? Convert.ToString(section["password"]) : null;
        config.HostId = section.ContainsKey("host_id") ? Convert.ToString(section["host_id"]) : "dime";
        config.GroupId = section.ContainsKey("group_id") ? Convert.ToString(section["group_id"]) : "dime";
        config.NodeId = section.ContainsKey("node_id") ? Convert.ToString(section["node_id"]) : "dime";
        config.DeviceId = section.ContainsKey("device_id") ? Convert.ToString(section["device_id"]) : "dime";
        config.ReconnectIntervalMs = section.ContainsKey("reconnect_interval") ? Convert.ToInt32(section["reconnect_interval"]) : 15000;
        config.BirthDelayMs = section.ContainsKey("birth_delay") ? Convert.ToInt32(section["birth_delay"]) : 10000;
        
        var connector = new Connectors.SparkplugB.Sink(config, disruptor);

        return connector;
    }
}