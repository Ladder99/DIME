using DIME.Connectors;
using DIME.Configuration.ModbusTcp;

namespace DIME.Configurator.ModbusTcp;

public static class Source
{
    public static IConnector Create(Dictionary<object, object> section, Disruptor.Dsl.Disruptor<MessageBoxMessage> disruptor)
    {
        ConnectorConfiguration config = ConfigurationHelper<ConnectorConfiguration, ConnectorItem>
            .MakeConfig(section, "ModbusTCP", Configuration.ConnectorDirectionEnum.Source);
        
        config.Address = section.ContainsKey("address") ? Convert.ToString(section["address"]) : string.Empty;
        config.Port = section.ContainsKey("port") ? Convert.ToInt32(section["port"]) : 502;
        config.Slave = section.ContainsKey("slave") ? Convert.ToByte(section["slave"]) : (byte)1;
        config.TimeoutMs = section.ContainsKey("timeout") ? Convert.ToInt32(section["timeout"]) : 1000;

        config.Items = ConfigurationHelper<ConnectorConfiguration, ConnectorItem>.MakeItems(config, section, (item, dict) =>
        {
            item.Type = dict.ContainsKey("type") ? Convert.ToInt32(dict["type"]) : 1;
            item.Count = dict.ContainsKey("count") ? Convert.ToUInt16(dict["count"]) : (ushort)1;
        });

        var connector = new Connectors.ModbusTcp.Source(config, disruptor);

        return connector;
    }
}