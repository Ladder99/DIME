using DIME.Connectors;
using DIME.Configuration.HttpClient;

namespace DIME.Configurator.HttpClient;

public static class Sink
{
    public static IConnector Create(Dictionary<object, object> section, Disruptor.Dsl.Disruptor<MessageBoxMessage> disruptor)
    {
        ConnectorConfiguration config = ConfigurationHelper<ConnectorConfiguration, ConnectorItem>
            .MakeConfig(section, "HTTPServer", Configuration.ConnectorDirectionEnum.Sink);

        config.Uri = section.ContainsKey("uri") ? Convert.ToString(section["uri"]) : "http://localhost/";
        
        var connector = new Connectors.HttpClient.Sink(config, disruptor);

        return connector;
    }
}