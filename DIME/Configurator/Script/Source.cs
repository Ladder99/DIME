using DIME.Connectors;
using DIME.Configuration.Script;

namespace DIME.Configurator.Script;

public static class Source
{
    public static IConnector Create(Dictionary<object, object> section, Disruptor.Dsl.Disruptor<MessageBoxMessage> disruptor)
    {
        ConnectorConfiguration config = ConfigurationHelper<ConnectorConfiguration, ConnectorItem>
            .MakeConfig(section, "Script", Configuration.ConnectorDirectionEnum.Source);
        
        config.Items = ConfigurationHelper<ConnectorConfiguration, ConnectorItem>.MakeItems(config, section, (item, dict) =>
        {
            
        });
        
        var connector = new Connectors.Script.Source(config, disruptor);

        return connector;
    }
}