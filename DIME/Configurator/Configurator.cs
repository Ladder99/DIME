using DIME.Connectors;
using Newtonsoft.Json;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace DIME.Configurator;

public partial class Configurator
{
    private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

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