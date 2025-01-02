using DIME.Configuration.TrakHoundHttp;
using DIME.Connectors;

namespace DIME.Configurator.TrakHoundHttp;

public static class Sink
{
    public static IConnector Create(Dictionary<object, object> section, Disruptor.Dsl.Disruptor<MessageBoxMessage> disruptor)
    {
        ConnectorConfiguration config = ConfigurationHelper<ConnectorConfiguration, ConnectorItem>
            .MakeConfig(section, "TrakHoundHTTP", Configuration.ConnectorDirectionEnum.Sink);

        config.Address = section.ContainsKey("address") ? Convert.ToString(section["address"]) : string.Empty;
        config.Port = section.ContainsKey("port") ? Convert.ToInt16(section["port"]) : 8472;
        config.UseSsl = section.ContainsKey("use_ssl") ? Convert.ToBoolean(section["use_ssl"]) : false;
        config.HostPath = section.ContainsKey("host_path") ? Convert.ToString(section["host_path"]) : null;
        config.Router = section.ContainsKey("router") ? Convert.ToString(section["router"]) : null;
        config.BasePath = section.ContainsKey("base_path") ? Convert.ToString(section["base_path"]) : null;
        
        var connector = new Connectors.TrakHoundHttp.Sink(config, disruptor);

        return connector;
    }
}