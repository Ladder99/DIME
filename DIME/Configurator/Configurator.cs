using DIME.Connectors;
using Newtonsoft.Json;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace DIME.Configurator;

public partial class Configurator
{
    private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

    public static Dictionary<object,object> Read(string[] configurationFilenames)
    {
        Logger.Debug("[Configurator.Read] Reading files {0}", configurationFilenames);
        var yaml = "";
        foreach (var configFile in configurationFilenames)
        {
            try
            {
                yaml += File.ReadAllText(configFile);
            }
            catch (Exception e)
            {
                Logger.Error(e, $"[Configurator.Read] Problem with {configFile}");
            }
            
        }
        Logger.Debug("[Configurator.Read] YAML \r\n{0}", yaml);
        var stringReader = new StringReader(yaml);
        var parser = new Parser(stringReader);
        var mergingParser = new MergingParser(parser);

        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

        Dictionary<object, object> dictionary = new();
        try
        {
            dictionary = deserializer.Deserialize<Dictionary<object, object>>(mergingParser);
        }
        catch (SemanticErrorException e)
        {
            Logger.Error(e, "[Configurator.Read] Error while parsing yaml.");
        }
        
        Logger.Debug("[Configurator.Read] Dictionary \r\n{0}", JsonConvert.SerializeObject(dictionary));
        return dictionary;
    }

    public static List<IConnector> CreateConnectors(Dictionary<object, object> configuration, Disruptor.Dsl.Disruptor<MessageBoxMessage> disruptor)
    {
        var _connectors = new List<IConnector>();

        if (configuration.ContainsKey("sinks"))
        {
            var sinks = configuration["sinks"] as List<object>;
            if (sinks is not null)
            {
                foreach (var section in sinks)
                {
                    var sectionDictionary = section as Dictionary<object, object>;
                    if (sectionDictionary is not null)
                    {
                        var connectorType = (sectionDictionary.ContainsKey("connector")
                            ? Convert.ToString(sectionDictionary["connector"])?.ToLower()
                            : "undefined");

                        var connector = SinkConnectorFactory.Create(connectorType, sectionDictionary, disruptor);

                        if (connector is null)
                        {
                            Logger.Error($"[Configurator.Sinks] Connector type is not supported: '{connectorType}'");
                        }
                        else if (connector.Configuration.Enabled)
                        {
                            _connectors.Add(connector);
                        }
                        else
                        {
                            Logger.Info($"[Configuration.Sinks] [{connector.Configuration.Name}] Connector is disabled.");
                        }
                    }
                    else
                    {
                        Logger.Warn($"[Configurator.Sinks] Configuration is invalid: '{section}'");
                    }
                }
            }
            else
            {
                Logger.Warn($"[Configurator.Sinks] Configuration key exists but it is empty.");
            }
        }
        else
        {
            Logger.Warn($"[Configurator.Sinks] Configuration key does not exist.");
        }

        if (configuration.ContainsKey("sources"))
        {
            var sources = configuration["sources"] as List<object>;
            if (sources is not null)
            {
                foreach (var section in sources)
                {
                    var sectionDictionary = section as Dictionary<object, object>;
                    if (sectionDictionary is not null)
                    {
                        var connectorType = (sectionDictionary.ContainsKey("connector")
                            ? Convert.ToString(sectionDictionary["connector"])?.ToLower()
                            : "undefined");

                        var connector = SourceConnectorFactory.Create(connectorType, sectionDictionary, disruptor);

                        if (connector is null)
                        {
                            Logger.Error($"[Configurator.Sources] Connector type is not supported: '{connectorType}'");
                        }
                        if (connector.Configuration.Enabled)
                        {
                            _connectors.Add(connector);
                        }
                        else
                        {
                            Logger.Info($"[Configurator.Sources] [{connector.Configuration.Name}] Connector is disabled.");
                        }
                    }
                    else
                    {
                        Logger.Warn($"[Configurator.Sinks] Configuration is invalid: '{section}'");
                    }
                }
            }
            else
            {
                Logger.Warn($"[Configurator.Sources] configuration key exists but it is empty.");
            }
        }
        else
        {
            Logger.Warn($"[Configurator.Sources] Configuration key does not exist.");
        }

        return _connectors;
    }
}