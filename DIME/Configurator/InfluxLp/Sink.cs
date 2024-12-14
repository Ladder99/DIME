using DIME.Connectors;
using DIME.Configuration.InfluxLp;

namespace DIME.Configurator.InfluxLp;

public static class Sink
{
    public static IConnector Create(Dictionary<object, object> section, Disruptor.Dsl.Disruptor<MessageBoxMessage> disruptor)
    {
        ConnectorConfiguration config = ConfigurationHelper<ConnectorConfiguration, ConnectorItem>
            .MakeConfig(section, "InfluxLP", Configuration.ConnectorDirectionEnum.Sink);

        config.Address = section.ContainsKey("address") ? Convert.ToString(section["address"]) : "127.0.0.1";
        config.Port = section.ContainsKey("port") ? Convert.ToInt32(section["port"]) : 8086;
        config.Token = section.ContainsKey("token") ? Convert.ToString(section["token"]) : string.Empty;
        config.BucketName = section.ContainsKey("bucket_name") ? Convert.ToString(section["bucket_name"]) : string.Empty;
        config.OrgId = section.ContainsKey("org_id") ? Convert.ToString(section["org_id"]) : string.Empty;
        
        var connector = new Connectors.InfluxLp.Sink(config, disruptor);

        return connector;
    }
}