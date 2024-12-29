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
        
        var connector = new Connectors.MtConnectAgent.Sink(config, disruptor);

        return connector;
    }
}