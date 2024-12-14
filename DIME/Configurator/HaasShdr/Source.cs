using DIME.Connectors;
using DIME.Configuration.HaasShdr;

namespace DIME.Configurator.HaasShdr;

public static class Source
{
    public static IConnector Create(Dictionary<object, object> section, Disruptor.Dsl.Disruptor<MessageBoxMessage> disruptor)
    {
        ConnectorConfiguration config = ConfigurationHelper<ConnectorConfiguration, ConnectorItem>
            .MakeConfig(section, "HaasSHDR", Configuration.ConnectorDirectionEnum.Source);
        
        config.IpAddress = section.ContainsKey("address") ? Convert.ToString(section["address"]) : "127.0.0.1";
        config.Port = section.ContainsKey("port") ? Convert.ToInt32(section["port"]) : 9998;
        config.TimeoutMs = section.ContainsKey("timeout") ? Convert.ToInt32(section["timeout"]) : 1000;
        config.HeartbeatMs = section.ContainsKey("heartbeat_interval") ? Convert.ToInt32(section["heartbeat_interval"]) : 4000;
        config.RetryMs = section.ContainsKey("retry_interval") ? Convert.ToInt32(section["retry_interval"]) : 10000;

        config.Items = ConfigurationHelper<ConnectorConfiguration, ConnectorItem>.MakeItems(config, section, (item, dict) =>
        {
            
        });
        
        var connector = new Connectors.HaasShdr.Source(config, disruptor);

        return connector;
    }
}