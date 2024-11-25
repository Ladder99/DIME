using IDS.Transporter.Connectors;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace IDS.Transporter.Configurator;

public partial class Configurator
{
    private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
    
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

        foreach (Dictionary<object, object> section in configuration["sinks"] as List<object>)
        {
            IConnector connector = null;
            
            switch (section["connector"].ToString().ToLower())
            {
                case "mqtt":
                    connector = Mqtt.Sink.Create(section);
                    break;
                default:
                    break;
            }

            if (connector.Configuration.Enabled)
            {
                _connectors.Add(connector);
            }
            else
            {
                Logger.Info($"[{connector.Configuration.Name}] Connector is disabled.");
            }
        }
        
        foreach (Dictionary<object, object> section in configuration["sources"] as List<object>)
        {
            IConnector connector = null;
            
            switch (section["connector"].ToString().ToLower())
            {
                case "ethernetip":
                    connector = EthernetIp.Source.Create(section);
                    break;
                case "mqtt":
                    connector = Mqtt.Source.Create(section);
                    break;
                default:
                    break;
            }

            if (connector.Configuration.Enabled)
            {
                _connectors.Add(connector);
            }
            else
            {
                Logger.Info($"[{connector.Configuration.Name}] Connector is disabled.");
            }
        }

        return _connectors;
    }

}