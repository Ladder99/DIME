using IDS.Transporter.Connectors;

namespace IDS.Transporter.Configurator;

public static class SourceConnectorFactory
{
    public static IConnector Create(string connectorType, Dictionary<object, object> section, Disruptor.Dsl.Disruptor<MessageBoxMessage> disruptor)
    {
        IConnector connector = null;
        
        switch (connectorType)
        {
            case "ethernetip":
                connector = EthernetIp.Source.Create(section, disruptor);
                break;
            case "haasshdr":
                connector = HaasShdr.Source.Create(section, disruptor);
                break;
            case "modbustcp":
                connector = ModbusTcp.Source.Create(section, disruptor);
                break;
            case "mqtt":
                connector = Mqtt.Source.Create(section, disruptor);
                break;
            
            default:
                break;
        }
        
        return connector;
    }
}