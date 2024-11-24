using ConsoleApp2.Connectors;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace ConsoleApp2.Configurator;

public partial class Configurator
{
    public static Dictionary<object,object> Read(string[] configurationFilenames)
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

    public static List<IConnector> CreateConnectors(Dictionary<object, object> configuration)
    {
        var _connectors = new List<IConnector>();

        foreach (Dictionary<object, object> section in configuration["sources"] as List<object>)
        {
            switch (section["connector"].ToString().ToLower())
            {
                case "ethernetip":
                    _connectors.Add(CreateEthernetIpConnector(section));
                    break;
                case "mqtt":
                    _connectors.Add(CreateMqttConnector(section));
                    break;
                default:
                    break;
            }
        }

        return _connectors;
    }

}