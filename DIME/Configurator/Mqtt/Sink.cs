using DIME.Connectors;
using DIME.Configuration.Mqtt;

namespace DIME.Configurator.Mqtt;

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
        config.Port = section.ContainsKey("port") ? Convert.ToInt32(section["port"]) : 1883;
        config.Username = section.ContainsKey("username") ? Convert.ToString(section["username"]) : string.Empty;
        config.Password = section.ContainsKey("password") ? Convert.ToString(section["password"]) : string.Empty;
        config.BaseTopic = section.ContainsKey("base_topic") ? Convert.ToString(section["base_topic"]) : "ids";
        config.QoS = section.ContainsKey("qos") ? Convert.ToInt32(section["qos"]) : 0;
        config.RetainPublish = section.ContainsKey("retain") ? Convert.ToBoolean(section["retain"]) : true;

        if (section.ContainsKey("exclude_filter"))
        {
            var filter = section["exclude_filter"] as List<object>;
            config.ExcludeFilter = filter is null ? new List<string>() : filter.Cast<string>().ToList();
        }
        
        if (section.ContainsKey("include_filter"))
        {
            var filter = section["include_filter"] as List<object>;
            config.IncludeFilter = filter is null ? new List<string>() : filter.Cast<string>().ToList();
        }
        
        var connector = new Connectors.Mqtt.Sink(config, disruptor);

        return connector;
    }
}