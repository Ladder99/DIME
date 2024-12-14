using DIME.Connectors;
using DIME.Configuration.MtConnectAgent;

namespace DIME.Configurator.MtConnectAgent;

public static class Source
{
    public static IConnector Create(Dictionary<object, object> section, Disruptor.Dsl.Disruptor<MessageBoxMessage> disruptor)
    {
        ConnectorConfiguration config = ConfigurationHelper<ConnectorConfiguration, ConnectorItem>
            .MakeConfig(section, "MTConnectAgent", Configuration.ConnectorDirectionEnum.Source);
        
        config.Address = section.ContainsKey("address") ? Convert.ToString(section["address"]) : "mtconnect.mazakcorp.com";
        config.Port = section.ContainsKey("port") ? Convert.ToInt32(section["port"]) : 5000;

        config.Items = ConfigurationHelper<ConnectorConfiguration, ConnectorItem>.MakeItems(config, section, (item, dict) =>
        {
            
        });

        var connector = new Connectors.MtConnectAgent.Source(config, disruptor);

        return connector;
    }
}