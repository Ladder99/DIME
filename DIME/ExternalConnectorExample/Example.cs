using DIME.Configuration;
using DIME.Connectors;

namespace DIME.ExternalConnectorExample;

public static class Example
{
    public static IConnector Create(Disruptor.Dsl.Disruptor<MessageBoxMessage> disruptor)
    {
        var configuration = new Configuration.ExternalConnectorExample.ConnectorConfiguration();
        configuration.Direction = ConnectorDirectionEnum.Source;
        configuration.ConnectorType = "ExternalConnectorExample";
        configuration.Items = new List<Configuration.ExternalConnectorExample.ConnectorItem>();
        configuration.Items.Add(new Configuration.ExternalConnectorExample.ConnectorItem()
        {
            Script = "return math.random(10000);"
        });

        return new Connectors.ExernalConnectorExample.Source(configuration, disruptor);
    }
}