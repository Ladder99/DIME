using DIME.Connectors;
using DIME.Configuration.MtConnectShdr;

namespace DIME.Configurator.MtConnectShdr;

public static class Sink
{
    public static IConnector Create(Dictionary<object, object> section, Disruptor.Dsl.Disruptor<MessageBoxMessage> disruptor)
    {
        ConnectorConfiguration config = ConfigurationHelper<ConnectorConfiguration, Configuration.ConnectorItem>
            .MakeConfig(section, "MTConnectSHDR", Configuration.ConnectorDirectionEnum.Sink);

        config.Port = section.ContainsKey("port") ? Convert.ToInt32(section["port"]) : 7878;
        config.DeviceKey = section.ContainsKey("device_key") ? Convert.ToString(section["device_key"]) : null;
        config.HeartbeatMs = section.ContainsKey("heartbeat_interval") ? Convert.ToInt32(section["heartbeat_interval"]) : 10000;
        config.FilterDuplicates = section.ContainsKey("filter_duplicates") ? Convert.ToBoolean(section["filter_duplicates"]) : true;
        config.OutputFolder = section.ContainsKey("output_folder") ? Convert.ToString(section["output_folder"]) : "./Output/MTConnect";
        
        var connector = new Connectors.MtConnectShdr.Sink(config, disruptor);

        return connector;
    }
}