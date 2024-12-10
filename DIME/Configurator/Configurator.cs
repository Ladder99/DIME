using DIME.Connectors;
using Newtonsoft.Json;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace DIME.Configurator;

public partial class Configurator
{
    private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

    private static IConnector CreateSink(object section, Disruptor.Dsl.Disruptor<MessageBoxMessage> disruptor)
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
                return connector;
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

        return null;
    }
    
    private static IConnector CreateSource(object section, Disruptor.Dsl.Disruptor<MessageBoxMessage> disruptor)
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
                return connector;
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

        return null;
    }
    
    private static List<IConnector> CreateSinks(Dictionary<object, object> configuration, Disruptor.Dsl.Disruptor<MessageBoxMessage> disruptor)
    {
        var connectors = new List<IConnector>();
        
        if (configuration.ContainsKey("sinks"))
        {
            var sinks = configuration["sinks"] as List<object>;
            if (sinks is not null)
            {
                foreach (var section in sinks)
                {
                    var connector = CreateSink(section, disruptor);
                    if (connector is not null)
                    {
                        connectors.Add(connector);
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
        
        return connectors;
    }
    
    private static List<IConnector> CreateSources(Dictionary<object, object> configuration, Disruptor.Dsl.Disruptor<MessageBoxMessage> disruptor)
    {
        var connectors = new List<IConnector>();
        
        if (configuration.ContainsKey("sources"))
        {
            var sources = configuration["sources"] as List<object>;
            if (sources is not null)
            {
                foreach (var section in sources)
                {
                    var connector = CreateSource(section, disruptor);
                    if (connector is not null)
                    {
                        connectors.Add(connector);
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
        
        return connectors;
    }
    
    public static List<IConnector> CreateConnectors(Dictionary<object, object> configuration, Disruptor.Dsl.Disruptor<MessageBoxMessage> disruptor)
    {
        return new[]
        {
            CreateSinks(configuration, disruptor), 
            CreateSources(configuration, disruptor)
        }.SelectMany(x => x).ToList();
    }
}