using DIME.Connectors;
using DIME.Configuration.HttpServer;

namespace DIME.Configurator.HttpServer;

public static class Sink
{
    public static IConnector Create(Dictionary<object, object> section, Disruptor.Dsl.Disruptor<MessageBoxMessage> disruptor)
    {
        ConnectorConfiguration config = ConfigurationHelper<ConnectorConfiguration, ConnectorItem>
            .MakeConfig(section, "HTTPServer", Configuration.ConnectorDirectionEnum.Sink);

        config.Uri = section.ContainsKey("uri") ? Convert.ToString(section["uri"]) : "http://localhost:8080/";
        
        var connector = new Connectors.HttpServer.Sink(config, disruptor);

        return connector;
    }
}