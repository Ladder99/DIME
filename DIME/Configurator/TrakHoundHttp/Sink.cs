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
        config.Hostname = section.ContainsKey("hostname") ? Convert.ToString(section["hostname"]) : "localhost";
        config.Port = section.ContainsKey("port") ? Convert.ToInt16(section["port"]) : 8472;
        config.UseSsl = section.ContainsKey("useSsl") ? Convert.ToBoolean(section["useSsl"]) : false;
        config.HostPath = section.ContainsKey("hostPath") ? Convert.ToString(section["hostPath"]) : null;
        config.Router = section.ContainsKey("router") ? Convert.ToString(section["router"]) : null;
        config.BasePath = section.ContainsKey("basePath") ? Convert.ToString(section["basePath"]) : null;
        var connector = new Connectors.TrakHoundHttp.Sink(config, disruptor);

        return connector;
    }
}