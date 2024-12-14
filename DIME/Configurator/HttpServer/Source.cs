using DIME.Connectors;
using DIME.Configuration.HttpServer;

namespace DIME.Configurator.HttpServer;

public static class Source
{
    public static IConnector Create(Dictionary<object, object> section, Disruptor.Dsl.Disruptor<MessageBoxMessage> disruptor)
    {
        ConnectorConfiguration config = ConfigurationHelper<ConnectorConfiguration, ConnectorItem>
            .MakeConfig(section, "HTTPServer", Configuration.ConnectorDirectionEnum.Source);
        
        config.Uri = section.ContainsKey("uri") ? Convert.ToString(section["uri"]) : "http://localhost:8081/";

        config.Items = ConfigurationHelper<ConnectorConfiguration, ConnectorItem>.MakeItems(config, section, (item, dict) =>
        {
            
        });

        var connector = new Connectors.HttpServer.Source(config, disruptor);

        return connector;
    }
}