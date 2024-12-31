using DIME.Connectors;
using DIME.Configuration.InfluxLp;

namespace DIME.Configurator.InfluxLp;

public static class Sink
{
    public static IConnector Create(Dictionary<object, object> section, Disruptor.Dsl.Disruptor<MessageBoxMessage> disruptor)
    {
        ConnectorConfiguration config = ConfigurationHelper<ConnectorConfiguration, ConnectorItem>
            .MakeConfig(section, "InfluxLP", Configuration.ConnectorDirectionEnum.Sink);

        config.Address = section.ContainsKey("address") ? Convert.ToString(section["address"]) : string.Empty;
        config.Token = section.ContainsKey("token") ? Convert.ToString(section["token"]) : string.Empty;
        config.Organization = section.ContainsKey("org_name") ? Convert.ToString(section["org_name"]) : null;
        config.BucketName = section.ContainsKey("bucket_name") ? Convert.ToString(section["bucket_name"]) : string.Empty;
        
        var connector = new Connectors.InfluxLp.Sink(config, disruptor);

        return connector;
    }
}