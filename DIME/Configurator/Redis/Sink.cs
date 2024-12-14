using DIME.Connectors;
using DIME.Configuration.Redis;

namespace DIME.Configurator.Redis;

public static class Sink
{
    public static IConnector Create(Dictionary<object, object> section, Disruptor.Dsl.Disruptor<MessageBoxMessage> disruptor)
    {
        ConnectorConfiguration config = new();
        config.ConnectorType = section.ContainsKey("connector") ? Convert.ToString(section["connector"]) : "MQTT";
        config.Direction = Configuration.ConnectorDirectionEnum.Sink;
        config.Enabled = section.ContainsKey("enabled") ? Convert.ToBoolean(section["enabled"]) : true;
        config.ScanIntervalMs = section.ContainsKey("scan_interval") ? Convert.ToInt32(section["scan_interval"]) : 1000;
        config.Name = section.ContainsKey("name") ? Convert.ToString(section["name"]) : Guid.NewGuid().ToString();
        config.Address = section.ContainsKey("address") ? Convert.ToString(section["address"]) : "127.0.0.1";
        config.Port = section.ContainsKey("port") ? Convert.ToInt32(section["port"]) : 6379;
        config.Database = section.ContainsKey("database") ? Convert.ToInt32(section["database"]) : 0;
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
        
        var connector = new Connectors.Redis.Sink(config, disruptor);

        return connector;
    }
}