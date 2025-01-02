using DIME.Connectors;
using DIME.Configuration.MongoDb;

namespace DIME.Configurator.MongoDb;

public static class Sink
{
    public static IConnector Create(Dictionary<object, object> section, Disruptor.Dsl.Disruptor<MessageBoxMessage> disruptor)
    {
        ConnectorConfiguration config = ConfigurationHelper<ConnectorConfiguration, ConnectorItem>
            .MakeConfig(section, "MongoDB", Configuration.ConnectorDirectionEnum.Sink);

        config.Address = section.ContainsKey("address") ? Convert.ToString(section["address"]) : string.Empty;
        config.Database = section.ContainsKey("database") ? Convert.ToString(section["database"]) : string.Empty;
        config.Collection = section.ContainsKey("collection") ? Convert.ToString(section["collection"]) : string.Empty;
        
        var connector = new Connectors.MongoDb.Sink(config, disruptor);

        return connector;
    }
}