using DIME.Connectors;
using DIME.Configuration.Snmp;

namespace DIME.Configurator.Snmp;

public static class Source
{
    public static IConnector Create(Dictionary<object, object> section, Disruptor.Dsl.Disruptor<MessageBoxMessage> disruptor)
    {
        ConnectorConfiguration config = ConfigurationHelper<ConnectorConfiguration, ConnectorItem>
            .MakeConfig(section, "SNMP", Configuration.ConnectorDirectionEnum.Source);
        
        config.Address = section.ContainsKey("address") ? Convert.ToString(section["address"]) : string.Empty;
        config.Port = section.ContainsKey("port") ? Convert.ToInt32(section["port"]) : 161;
        config.Community = section.ContainsKey("community") ? Convert.ToString(section["community"]) : "public";
        config.TimeoutMs = section.ContainsKey("timeout") ? Convert.ToInt32(section["timeout"]) : 1000;
        
        config.Items = ConfigurationHelper<ConnectorConfiguration, ConnectorItem>.MakeItems(config, section, (item, dict) =>
        {
            
        });
        
        var connector = new Connectors.Snmp.Source(config, disruptor);

        return connector;
    }
}