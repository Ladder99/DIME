using IDS.Transporter.Connectors;
using IDS.Transporter.Configuration.EthernetIp;

namespace IDS.Transporter.Configurator.EthernetIp;

public static class Source
{
    public static IConnector Create(Dictionary<object, object> section, Disruptor.Dsl.Disruptor<MessageBoxMessage> disruptor)
    {
        ConnectorConfiguration config = new();
        config.ConnectorType = section.ContainsKey("connector") ? Convert.ToString(section["connector"]) : "EthernetIP";
        config.Direction = Configuration.ConnectorDirectionEnum.Source;
        config.Enabled = section.ContainsKey("enabled") ? Convert.ToBoolean(section["enabled"]) : true;
        config.ScanIntervalMs = section.ContainsKey("scan_interval") ? Convert.ToInt32(section["scan_interval"]) : 1000;
        config.ReportByException = section.ContainsKey("rbe") ? Convert.ToBoolean(section["rbe"]) : true;
        config.Name = section.ContainsKey("name") ? Convert.ToString(section["name"]) : Guid.NewGuid().ToString();
        config.InitScript = section.ContainsKey("init_script") ? Convert.ToString(section["init_script"]) : null;
        config.PlcType = section.ContainsKey("type") ? Convert.ToInt32(section["type"]) : 0;
        config.IpAddress = section.ContainsKey("address") ? Convert.ToString(section["address"]) : "0.0.0.0";
        config.Path = section.ContainsKey("path") ? Convert.ToString(section["path"]) : "1,0";
        config.Log = section.ContainsKey("log") ? Convert.ToInt32(section["log"]) : 0;
        config.TimeoutMs = section.ContainsKey("timeout") ? Convert.ToInt32(section["timeout"]) : 1000;
        config.Items = new List<ConnectorItem>();

        if (section.ContainsKey("items"))
        {
            var items = section["items"] as List<object>;
            if (items != null)
            {
                foreach (var item in items)
                {
                    var itemDictionary = item as Dictionary<object, object>;
                    if (itemDictionary != null)
                    {
                        config.Items.Add(new ConnectorItem()
                        {
                            Enabled = itemDictionary.ContainsKey("enabled") ? Convert.ToBoolean(itemDictionary["enabled"]) : true,
                            Name = itemDictionary.ContainsKey("name") ? Convert.ToString(itemDictionary["name"]) : Guid.NewGuid().ToString(),
                            Script = itemDictionary.ContainsKey("script") ? Convert.ToString(itemDictionary["script"]) : null,
                            Type = itemDictionary.ContainsKey("type") ? Convert.ToString(itemDictionary["type"]) : null,
                            Address = itemDictionary.ContainsKey("address") ? Convert.ToString(itemDictionary["address"]) : null
                        });
                    }
                }
            }
        }
        
        var connector = new Connectors.EthernetIp.Source(config, disruptor);

        return connector;
    }
}