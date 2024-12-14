using DIME.Connectors;
using DIME.Configuration.SparkplugB;

namespace DIME.Configurator.SparkplugB;

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
        config.Username = section.ContainsKey("username") ? Convert.ToString(section["username"]) : null;
        config.Password = section.ContainsKey("password") ? Convert.ToString(section["password"]) : null;
        config.HostId = section.ContainsKey("host_id") ? Convert.ToString(section["host_id"]) : "dime";
        config.GroupId = section.ContainsKey("group_id") ? Convert.ToString(section["group_id"]) : "dime";
        config.NodeId = section.ContainsKey("node_id") ? Convert.ToString(section["node_id"]) : "dime";
        config.DeviceId = section.ContainsKey("device_id") ? Convert.ToString(section["device_id"]) : "dime";
        config.ReconnectIntervalMs = section.ContainsKey("reconnect_interval") ? Convert.ToInt32(section["reconnect_interval"]) : 15000;
        config.BirthDelayMs = section.ContainsKey("birth_delay") ? Convert.ToInt32(section["birth_delay"]) : 10000;
        
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
        
        var connector = new Connectors.SparkplugB.Sink(config, disruptor);

        return connector;
    }
}