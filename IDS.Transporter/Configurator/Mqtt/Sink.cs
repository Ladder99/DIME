using IDS.Transporter.Connectors;
using IDS.Transporter.Configuration.Mqtt;

namespace IDS.Transporter.Configurator.Mqtt;

public static class Sink
{
    public static IConnector Create(Dictionary<object, object> section)
    {
        var config = new ConnectorConfiguration();
        config.ConnectorType = Convert.ToString(section["connector"]);
        config.Direction = Configuration.ConnectorDirectionEnum.Sink;
        config.Enabled = Convert.ToBoolean(section["enabled"]);
        config.ScanInterval = Convert.ToInt32(section["scan_interval"]);
        config.Name = Convert.ToString(section["name"]);
        config.IpAddress = Convert.ToString(section["address"]);
        config.Port = Convert.ToInt32(section["port"]);
        config.BaseTopic = Convert.ToString(section["base_topic"]);

        var connector = new Connectors.Mqtt.Sink(config);

        return connector;
    }
}