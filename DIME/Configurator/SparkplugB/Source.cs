using DIME.Connectors;
using DIME.Configuration.SparkplugB;

namespace DIME.Configurator.SparkplugB;

public static class Source
{
    public static IConnector Create(Dictionary<object, object> section, Disruptor.Dsl.Disruptor<MessageBoxMessage> disruptor)
    {
        ConnectorConfiguration config = ConfigurationHelper<ConnectorConfiguration, ConnectorItem>
            .MakeConfig(section, "MQTT", Configuration.ConnectorDirectionEnum.Source);
        
        config.Address = section.ContainsKey("address") ? Convert.ToString(section["address"]) : string.Empty;
        config.Port = section.ContainsKey("port") ? Convert.ToInt32(section["port"]) : 1883;
        config.Username = section.ContainsKey("username") ? Convert.ToString(section["username"]) : null;
        config.Password = section.ContainsKey("password") ? Convert.ToString(section["password"]) : null;
        config.CleanSession = section.ContainsKey("clean_session") ? Convert.ToBoolean(section["clean_session"]) : true;
        config.QoS = section.ContainsKey("qos") ? Convert.ToInt32(section["qos"]) : 0;
        
        config.Items = ConfigurationHelper<ConnectorConfiguration, ConnectorItem>.MakeItems(config, section, (item, dict) =>
        {
            
        });

        var connector = new Connectors.SparkplugB.Source(config, disruptor);

        return connector;
    }
}