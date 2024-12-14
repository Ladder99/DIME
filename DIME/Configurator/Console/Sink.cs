using DIME.Connectors;
using DIME.Configuration.Console;

namespace DIME.Configurator.Console;

public static class Sink
{
    public static IConnector Create(Dictionary<object, object> section, Disruptor.Dsl.Disruptor<MessageBoxMessage> disruptor)
    {
        ConnectorConfiguration config = ConfigurationHelper<ConnectorConfiguration, ConnectorItem>
            .MakeConfig(section, "Console", Configuration.ConnectorDirectionEnum.Sink);
        
        
        var connector = new Connectors.Console.Sink(config, disruptor);

        return connector;
    }
}