using DIME.Configuration.SplunkHec;
using DIME.Connectors;

namespace DIME.Configurator.SplunkHec;

public static class Sink
{
    public static IConnector Create(Dictionary<object, object> section, Disruptor.Dsl.Disruptor<MessageBoxMessage> disruptor)
    {
        ConnectorConfiguration config = new();
        config.ConnectorType = section.ContainsKey("connector") ? Convert.ToString(section["connector"]) : "TrakHoundHttp";
        config.Direction = Configuration.ConnectorDirectionEnum.Sink;
        config.Enabled = section.ContainsKey("enabled") ? Convert.ToBoolean(section["enabled"]) : true;
        config.ScanIntervalMs = section.ContainsKey("scan_interval") ? Convert.ToInt32(section["scan_interval"]) : 1000;
        config.Name = section.ContainsKey("name") ? Convert.ToString(section["name"]) : Guid.NewGuid().ToString();
        config.Address = section.ContainsKey("address") ? Convert.ToString(section["address"]) : "localhost";
        config.Port = section.ContainsKey("port") ? Convert.ToInt16(section["port"]) : 8472;
        config.UseSsl = section.ContainsKey("use_ssl") ? Convert.ToBoolean(section["use_ssl"]) : false;
        config.Token = section.ContainsKey("token") ? Convert.ToString(section["token"]) : string.Empty;
        config.EventOrMetric = section.ContainsKey("event_or_metric") ? Convert.ToString(section["event_or_metric"]) : "event";
        config.Source = section.ContainsKey("source") ? Convert.ToString(section["source"]) : string.Empty;
        config.SourceType = section.ContainsKey("source_type") ? Convert.ToString(section["source_type"]) : "_json";
        config.ExcludeFilter = new List<string>();
        config.IncludeFilter = new List<string>();
        
        if (section.ContainsKey("exclude_filter") && section["exclude_filter"] as List<object> is not null)
        {
            config.ExcludeFilter = (section["exclude_filter"] as List<object>).Cast<string>().ToList();
        }
        
        if (section.ContainsKey("include_filter") && section["include_filter"] as List<object> is not null)
        {
            config.ExcludeFilter = (section["include_filter"] as List<object>).Cast<string>().ToList();
        }
        
        var connector = new Connectors.SplunkHec.Sink(config, disruptor);

        return connector;
    }
}