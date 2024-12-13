using DIME.Configuration.SplunkEhSdk;
using DIME.Connectors;

namespace DIME.Configurator.SplunkEhSdk;

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
        config.Address = section.ContainsKey("address") ? Convert.ToString(section["address"]) : "127.0.0.1";
        config.Port = section.ContainsKey("port") ? Convert.ToInt32(section["port"]) : 50051;
        config.NumbersToMetrics = section.ContainsKey("numbers_to_metrics") ? Convert.ToBoolean(section["numbers_to_metrics"]) : false;
        var connector = new Connectors.SplunkEhSdk.Sink(config, disruptor);

        return connector;
    }
}