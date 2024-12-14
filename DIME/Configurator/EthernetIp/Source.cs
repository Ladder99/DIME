using DIME.Connectors;
using DIME.Configuration.EthernetIp;

namespace DIME.Configurator.EthernetIp;

public static class Source
{
    public static IConnector Create(Dictionary<object, object> section, Disruptor.Dsl.Disruptor<MessageBoxMessage> disruptor)
    {
        ConnectorConfiguration config = ConfigurationHelper<ConnectorConfiguration, ConnectorItem>
            .MakeConfig(section, "EthernetIP", Configuration.ConnectorDirectionEnum.Source);
        
        config.Address = section.ContainsKey("address") ? Convert.ToString(section["address"]) : "127.0.0.1";
        config.PlcType = section.ContainsKey("type") ? Convert.ToInt32(section["type"]) : 0;
        config.Address = section.ContainsKey("address") ? Convert.ToString(section["address"]) : "0.0.0.0";
        config.Path = section.ContainsKey("path") ? Convert.ToString(section["path"]) : "1,0";
        config.Log = section.ContainsKey("log") ? Convert.ToInt32(section["log"]) : 0;
        config.TimeoutMs = section.ContainsKey("timeout") ? Convert.ToInt32(section["timeout"]) : 1000;
        config.BypassPing = section.ContainsKey("bypass_ping") ? Convert.ToBoolean(section["bypass_ping"]) : false;

        config.Items = ConfigurationHelper<ConnectorConfiguration, ConnectorItem>.MakeItems(config, section, (item, dict) =>
        {
            item.Type = dict.ContainsKey("type") ? Convert.ToString(dict["type"]) : null;
        });
        
        var connector = new Connectors.EthernetIp.Source(config, disruptor);

        return connector;
    }
}