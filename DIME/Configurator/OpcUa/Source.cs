using DIME.Connectors;
using DIME.Configuration.OpcUa;

namespace DIME.Configurator.OpcUa;

public static class Source
{
    public static IConnector Create(Dictionary<object, object> section, Disruptor.Dsl.Disruptor<MessageBoxMessage> disruptor)
    {
        ConnectorConfiguration config = ConfigurationHelper<ConnectorConfiguration, ConnectorItem>
            .MakeConfig(section, "OpcUA", Configuration.ConnectorDirectionEnum.Source);
        
        config.Address = section.ContainsKey("address") ? Convert.ToString(section["address"]) : "127.0.0.1";
        config.Port = section.ContainsKey("port") ? Convert.ToInt32(section["port"]) : 49320;
        config.TimeoutMs = section.ContainsKey("timeout") ? Convert.ToInt32(section["timeout"]) : 1000;
        config.UseAnonymousUser = section.ContainsKey("anonymous") ? Convert.ToBoolean(section["anonymous"]) : false;
        config.Username = section.ContainsKey("username") ? Convert.ToString(section["username"]) : string.Empty;
        config.Password = section.ContainsKey("password") ? Convert.ToString(section["password"]) : string.Empty;

        config.Items = ConfigurationHelper<ConnectorConfiguration, ConnectorItem>.MakeItems(config, section, (item, dict) =>
        {
            item.Namespace = dict.ContainsKey("namespace") ? Convert.ToUInt16(dict["namespace"]) : (ushort)2;
        });
        
        var connector = new Connectors.OpcUa.Source(config, disruptor);

        return connector;
    }
}