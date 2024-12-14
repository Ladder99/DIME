using DIME.Configuration.TrakHoundHttp;
using DIME.Connectors;

namespace DIME.Configurator.TrakHoundHttp;

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
        config.HostPath = section.ContainsKey("host_path") ? Convert.ToString(section["host_path"]) : null;
        config.Router = section.ContainsKey("router") ? Convert.ToString(section["router"]) : null;
        config.BasePath = section.ContainsKey("base_path") ? Convert.ToString(section["base_path"]) : null;
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
        
        var connector = new Connectors.TrakHoundHttp.Sink(config, disruptor);

        return connector;
    }
}