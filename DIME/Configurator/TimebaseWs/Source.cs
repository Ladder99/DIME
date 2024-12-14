using DIME.Connectors;
using DIME.Configuration.TimebaseWs;

namespace DIME.Configurator.TimebaseWs;


public static class Source
{
    public static IConnector Create(Dictionary<object, object> section, Disruptor.Dsl.Disruptor<MessageBoxMessage> disruptor)
    {
        ConnectorConfiguration config = ConfigurationHelper<ConnectorConfiguration, ConnectorItem>
            .MakeConfig(section, "TimebaseWS", Configuration.ConnectorDirectionEnum.Source);
        
        config.Address = section.ContainsKey("address") ? Convert.ToString(section["address"]) : "127.0.0.1";
        config.Port = section.ContainsKey("port") ? Convert.ToInt32(section["port"]) : 4511;
        
        config.Items = ConfigurationHelper<ConnectorConfiguration, ConnectorItem>.MakeItems(config, section, (item, dict) =>
        {
            item.Group = dict.ContainsKey("group") ? Convert.ToString(dict["group"]) : null;
        });

        var connector = new Connectors.TimebaseWs.Source(config, disruptor);

        return connector;
    }
}