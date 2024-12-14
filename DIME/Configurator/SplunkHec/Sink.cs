using DIME.Configuration.SplunkHec;
using DIME.Connectors;

namespace DIME.Configurator.SplunkHec;

public static class Sink
{
    public static IConnector Create(Dictionary<object, object> section, Disruptor.Dsl.Disruptor<MessageBoxMessage> disruptor)
    {
        ConnectorConfiguration config = ConfigurationHelper<ConnectorConfiguration, ConnectorItem>
            .MakeConfig(section, "SplunkHEC", Configuration.ConnectorDirectionEnum.Sink);

        config.Address = section.ContainsKey("address") ? Convert.ToString(section["address"]) : "127.0.0.1";
        config.Port = section.ContainsKey("port") ? Convert.ToInt16(section["port"]) : 8472;
        config.UseSsl = section.ContainsKey("use_ssl") ? Convert.ToBoolean(section["use_ssl"]) : false;
        config.Token = section.ContainsKey("token") ? Convert.ToString(section["token"]) : string.Empty;
        config.EventOrMetric = section.ContainsKey("event_or_metric") ? Convert.ToString(section["event_or_metric"]) : "event";
        config.Source = section.ContainsKey("source") ? Convert.ToString(section["source"]) : string.Empty;
        config.SourceType = section.ContainsKey("source_type") ? Convert.ToString(section["source_type"]) : "_json";
        
        var connector = new Connectors.SplunkHec.Sink(config, disruptor);

        return connector;
    }
}