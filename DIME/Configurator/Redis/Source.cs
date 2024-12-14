using DIME.Connectors;
using DIME.Configuration.Redis;

namespace DIME.Configurator.Redis;

public static class Source
{
    public static IConnector Create(Dictionary<object, object> section, Disruptor.Dsl.Disruptor<MessageBoxMessage> disruptor)
    {
        ConnectorConfiguration config = ConfigurationHelper<ConnectorConfiguration, ConnectorItem>
            .MakeConfig(section, "Redis", Configuration.ConnectorDirectionEnum.Source);
        
        config.Address = section.ContainsKey("address") ? Convert.ToString(section["address"]) : "127.0.0.1";
        config.Port = section.ContainsKey("port") ? Convert.ToInt32(section["port"]) : 6379;
        config.Database = section.ContainsKey("database") ? Convert.ToInt32(section["database"]) : 0;

        config.Items = ConfigurationHelper<ConnectorConfiguration, ConnectorItem>.MakeItems(config, section, (item, dict) =>
        {
            
        });
        
        var connector = new Connectors.Redis.Source(config, disruptor);

        return connector;
    }
}