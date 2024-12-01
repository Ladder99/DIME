using IDS.Transporter.Connectors;

namespace IDS.Transporter.Configurator;

public static class SinkConnectorFactory
{
    public static IConnector Create(string connectorType, Dictionary<object, object> section, Disruptor.Dsl.Disruptor<MessageBoxMessage> disruptor)
    {
        IConnector connector = null;
        
        switch (connectorType)
        {
            case "mqtt":
                connector = Mqtt.Sink.Create(section, disruptor);
                break;
            case "mtconnectshdr":
                connector = MtConnectShdr.Sink.Create(section, disruptor);
                break;
            case "mtconnectagent":
                connector = MtConnectAgent.Sink.Create(section, disruptor);
                break;
            default:
                break;
        }
        
        return connector;
    }
}