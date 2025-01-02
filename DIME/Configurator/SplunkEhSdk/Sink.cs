using DIME.Configuration.SplunkEhSdk;
using DIME.Connectors;

namespace DIME.Configurator.SplunkEhSdk;

public static class Sink
{
    public static IConnector Create(Dictionary<object, object> section, Disruptor.Dsl.Disruptor<MessageBoxMessage> disruptor)
    {
        ConnectorConfiguration config = ConfigurationHelper<ConnectorConfiguration, ConnectorItem>
            .MakeConfig(section, "SplunkEhSDK", Configuration.ConnectorDirectionEnum.Sink);

        config.Address = section.ContainsKey("address") ? Convert.ToString(section["address"]) : "http://host.docker.internal";
        config.Port = section.ContainsKey("port") ? Convert.ToInt32(section["port"]) : 50051;
        config.NumbersToMetrics = section.ContainsKey("numbers_to_metrics") ? Convert.ToBoolean(section["numbers_to_metrics"]) : false;
        
        var connector = new Connectors.SplunkEhSdk.Sink(config, disruptor);

        return connector;
    }
}