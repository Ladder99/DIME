using DIME.Connectors;
using DIME.Configuration.Mqtt;

namespace DIME.Configurator.Mqtt;

public static class Sink
{
    public static IConnector Create(Dictionary<object, object> section, Disruptor.Dsl.Disruptor<MessageBoxMessage> disruptor)
    {
        ConnectorConfiguration config = ConfigurationHelper<ConnectorConfiguration, ConnectorItem>
            .MakeConfig(section, "MQTT", Configuration.ConnectorDirectionEnum.Sink);
        
        config.Address = section.ContainsKey("address") ? Convert.ToString(section["address"]) : string.Empty;
        config.Port = section.ContainsKey("port") ? Convert.ToInt32(section["port"]) : 1883;
        config.Username = section.ContainsKey("username") ? Convert.ToString(section["username"]) : string.Empty;
        config.Password = section.ContainsKey("password") ? Convert.ToString(section["password"]) : string.Empty;
        config.BaseTopic = section.ContainsKey("base_topic") ? Convert.ToString(section["base_topic"]) : "dime";
        config.QoS = section.ContainsKey("qos") ? Convert.ToInt32(section["qos"]) : 0;
        config.RetainPublish = section.ContainsKey("retain") ? Convert.ToBoolean(section["retain"]) : true;
        
        var connector = new Connectors.Mqtt.Sink(config, disruptor);

        return connector;
    }
}