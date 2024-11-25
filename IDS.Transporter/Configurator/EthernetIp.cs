using IDS.Transporter.Connectors;
using IDS.Transporter.Configuration.EthernetIp;

namespace IDS.Transporter.Configurator;

public partial class Configurator
{
    public static IConnector CreateEthernetIpConnector(Dictionary<object, object> section)
    {
        var config = new ConnectorConfiguration();
        config.ConnectorType = Convert.ToString(section["connector"]);
        config.Direction = Configuration.ConnectorDirectionEnum.Source;
        config.Enabled = Convert.ToBoolean(section["enabled"]);
        config.ScanInterval = Convert.ToInt32(section["scan_interval"]);
        config.Name = Convert.ToString(section["name"]);
        config.PlcType = Convert.ToInt32(section["type"]);
        config.IpAddress = Convert.ToString(section["address"]);
        config.Path = Convert.ToString(section["path"]);
        config.Log = Convert.ToInt32(section["log"]);
        config.Timeout = Convert.ToInt32(section["timeout"]);
        config.Items = new List<ConnectorItem>();

        foreach (Dictionary<object, object> item in section["items"] as List<object>)
        {
            config.Items.Add(new ConnectorItem()
            {
                Enabled = Convert.ToBoolean(item["enabled"]),
                Name = Convert.ToString(item["name"]),
                Type = Convert.ToString(item["type"]),
                Address = Convert.ToString(item["address"])
            });
        }

        var connector = new Connectors.EthernetIp.Source(config);

        return connector;
    }
}