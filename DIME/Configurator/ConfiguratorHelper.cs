using DIME.Configuration;

namespace DIME.Configurator;

public static class ConfigurationHelper<TConfig, TItem>
    where TConfig : Configuration.ConnectorConfiguration<TItem>
    where TItem : Configuration.ConnectorItem
{
    public static TConfig MakeConfig(Dictionary<object, object> section, string connectorType, ConnectorDirectionEnum direction)
    {
        var instance = Activator.CreateInstance<TConfig>();
        instance.Direction = direction;
        instance.Enabled = section.ContainsKey("enabled") ? Convert.ToBoolean(section["enabled"]) : true;
        instance.ScanIntervalMs = section.ContainsKey("scan_interval") ? Convert.ToInt32(section["scan_interval"]) : 1000;
        instance.ReportByException = section.ContainsKey("rbe") ? Convert.ToBoolean(section["rbe"]) : true;
        instance.Name = section.ContainsKey("name") ? Convert.ToString(section["name"]) : Guid.NewGuid().ToString();
        instance.InitScript = section.ContainsKey("init_script") ? Convert.ToString(section["init_script"]) : null;
        instance.DeinitScript = section.ContainsKey("deinit_script") ? Convert.ToString(section["deinit_script"]) : null;
        instance.LoopEnterScript = section.ContainsKey("enter_script") ? Convert.ToString(section["enter_script"]) : null;
        instance.LoopExitScript = section.ContainsKey("exit_script") ? Convert.ToString(section["exit_script"]) : null;
        instance.ItemizedRead = section.ContainsKey("itemized_read") ? Convert.ToBoolean(section["itemized_read"]) : false;
        instance.ConnectorType = connectorType;
        instance.Items = new List<TItem>();
        instance.ExcludeFilter = new List<string>();
        instance.IncludeFilter = new List<string>();
        instance.SinkMeta = section.ContainsKey("sink") ? section["sink"] as Dictionary<object, object> : new Dictionary<object, object>();
        instance.StripPathPrefix = section.ContainsKey("strip_path_prefix") ? Convert.ToBoolean(section["strip_path_prefix"]) : false;
        instance.UseSinkTransform = section.ContainsKey("use_sink_transform") ? Convert.ToBoolean(section["use_sink_transform"]) : false;
        
        if (section.ContainsKey("exclude_filter") && section["exclude_filter"] as List<object> is not null)
        {
            instance.ExcludeFilter = (section["exclude_filter"] as List<object>).Cast<string>().ToList();
        }
        
        if (section.ContainsKey("include_filter") && section["include_filter"] as List<object> is not null)
        {
            instance.IncludeFilter = (section["include_filter"] as List<object>).Cast<string>().ToList();
        }
        
        return instance;
    }

    public static List<TItem> MakeItems(TConfig configuration, Dictionary<object, object> section, Action<TItem,Dictionary<object,object>> editCallback)
    {
        List<TItem> connectorItems = new List<TItem>();
        
        if (section.ContainsKey("items"))
        {
            var items = section["items"] as List<object>;
            if (items is not null)
            {
                foreach (var item in items)
                {
                    var itemDictionary = item as Dictionary<object, object>;
                    if (itemDictionary is not null)
                    {
                        var connectorItem = Activator.CreateInstance<TItem>();
                        connectorItem.Configuration = configuration;
                        connectorItem.Enabled = itemDictionary.ContainsKey("enabled") ? Convert.ToBoolean(itemDictionary["enabled"]) : true;
                        connectorItem.Name = itemDictionary.ContainsKey("name") ? Convert.ToString(itemDictionary["name"]) : Guid.NewGuid().ToString();
                        connectorItem.ReportByException = itemDictionary.ContainsKey("rbe") ? Convert.ToBoolean(itemDictionary["rbe"]) : configuration.ReportByException;
                        connectorItem.Address = itemDictionary.ContainsKey("address") ? Convert.ToString(itemDictionary["address"]) : null;
                        connectorItem.Script = itemDictionary.ContainsKey("script") ? Convert.ToString(itemDictionary["script"]) : null;
                        connectorItem.SinkMeta = itemDictionary.ContainsKey("sink") ? itemDictionary["sink"] as Dictionary<object, object> : new Dictionary<object, object>();
                        editCallback(connectorItem, itemDictionary);
                        connectorItems.Add(connectorItem);
                    };
                }
            }
        }

        return connectorItems;
    }
}