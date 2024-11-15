using System.Collections.Generic;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace ConsoleApp2;

public class ConfigurationBag
{
    public PropertyBag Connector { get; set; }
    public List<PropertyBag> Items { get; set; }
}

public class Config
{
    public Dictionary<object,object> Read(string[] configurationFilenames)
    {
        var yaml = "";
        foreach (var configFile in configurationFilenames) yaml += File.ReadAllText(configFile);

        var stringReader = new StringReader(yaml);
        var parser = new Parser(stringReader);
        var mergingParser = new MergingParser(parser);

        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

        return deserializer.Deserialize(mergingParser) as Dictionary<object, object>;
    }

    public List<ConfigurationBag> CreateSourceBags(Dictionary<object, object> configuration)
    {
        var bags = new List<ConfigurationBag>();
        
        foreach (Dictionary<object, object> source in configuration["sources"] as List<object>)
        {
            var connectorConfiguration = new PropertyBag();
            var itemsConfiguration = new List<PropertyBag>();
    
            foreach (KeyValuePair<object, object> kvp in source)
            {
                if (kvp.Key.ToString() != "items")
                {
                    connectorConfiguration.SetProperty(kvp.Key.ToString(), kvp.Value);
                }
            }

            foreach (Dictionary<object, object> item in source["items"] as List<object>)
            {
                var itemConfiguration = new PropertyBag();
        
                foreach (KeyValuePair<object, object> kvp in item)
                {
                    itemConfiguration.SetProperty(kvp.Key.ToString(), kvp.Value);
                }
        
                itemsConfiguration.Add(itemConfiguration);
            }
            
            bags.Add(new ConfigurationBag()
            {
                Connector = connectorConfiguration,
                Items = itemsConfiguration
            });
        }

        return bags;
    }
    
    public List<ConfigurationBag> CreateSinkBags(Dictionary<object, object> configuration)
    {
        var bags = new List<ConfigurationBag>();
        
        foreach (Dictionary<object, object> source in configuration["sinks"] as List<object>)
        {
            var connectorConfiguration = new PropertyBag();
    
            foreach (KeyValuePair<object, object> kvp in source)
            {
                if (kvp.Key.ToString() != "items")
                {
                    connectorConfiguration.SetProperty(kvp.Key.ToString(), kvp.Value);
                }
            }
            
            bags.Add(new ConfigurationBag()
            {
                Connector = connectorConfiguration,
                Items = new List<PropertyBag>()
            });
        }

        return bags;
    }
}