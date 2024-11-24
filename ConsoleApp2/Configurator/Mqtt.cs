using ConsoleApp2.Connectors;
using ConsoleApp2.Configuration.Mqtt;

namespace ConsoleApp2.Configurator;

public partial class Configurator
{
    public static IConnector CreateMqttConnector(Dictionary<object, object> section)
    {
        var config = new ConnectorConfiguration();
        config.ConnectorType = Convert.ToString(section["connector"]);
        config.Direction = Configuration.ConnectorDirectionEnum.Source;
        config.Enabled = Convert.ToBoolean(section["enabled"]);
        config.ScanInterval = Convert.ToInt32(section["scan_interval"]);
        config.Name = Convert.ToString(section["name"]);
        config.IpAddress = Convert.ToString(section["address"]);
        config.Port = Convert.ToInt32(section["port"]);
        config.Items = new List<ConnectorItem>();

        foreach (Dictionary<object, object> item in section["items"] as List<object>)
        {
            config.Items.Add(new ConnectorItem()
            {
                Enabled = Convert.ToBoolean(item["enabled"]),
                Name = Convert.ToString(item["name"]),
                Address = Convert.ToString(item["address"])
            });
        }

        var connector = new Connectors.Mqtt.Source(config);

        return connector;
    }
}