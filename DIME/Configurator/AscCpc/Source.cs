using DIME.Connectors;
using DIME.Configuration.AscCpc;

namespace DIME.Configurator.AscCpc;

public static class Source
{
    public static IConnector Create(Dictionary<object, object> section, Disruptor.Dsl.Disruptor<MessageBoxMessage> disruptor)
    {
        ConnectorConfiguration config = ConfigurationHelper<ConnectorConfiguration, ConnectorItem>
            .MakeConfig(section, "AscCPC", Configuration.ConnectorDirectionEnum.Source);
        
        config.Address = section.ContainsKey("address") ? Convert.ToString(section["address"]) : string.Empty;
        config.Port = section.ContainsKey("port") ? Convert.ToInt32(section["port"]) : 9999;
        config.BypassPing = section.ContainsKey("bypass_ping") ? Convert.ToBoolean(section["bypass_ping"]) : false;

        config.Items = ConfigurationHelper<ConnectorConfiguration, ConnectorItem>.MakeItems(config, section, (item, dict) =>
        {
            
        });
        
        var connector = new Connectors.AscCpc.Source(config, disruptor);

        return connector;
    }
}