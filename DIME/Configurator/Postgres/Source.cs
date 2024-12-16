using DIME.Connectors;
using DIME.Configuration.Postgres;

namespace DIME.Configurator.Postgres;

public static class Source
{
    public static IConnector Create(Dictionary<object, object> section, Disruptor.Dsl.Disruptor<MessageBoxMessage> disruptor)
    {
        ConnectorConfiguration config = ConfigurationHelper<ConnectorConfiguration, ConnectorItem>
            .MakeConfig(section, "Script", Configuration.ConnectorDirectionEnum.Source);
        
        config.ConnectionString = section.ContainsKey("connection_string") ? Convert.ToString(section["connection_string"]) : string.Empty;
        config.CommandText = section.ContainsKey("command_text") ? Convert.ToString(section["command_text"]) : string.Empty;
        
        config.Items = ConfigurationHelper<ConnectorConfiguration, ConnectorItem>.MakeItems(config, section, (item, dict) =>
        {
            
        });
        
        var connector = new Connectors.Postgres.Source(config, disruptor);

        return connector;
    }
}