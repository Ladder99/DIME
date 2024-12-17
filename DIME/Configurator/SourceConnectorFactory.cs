using DIME.Connectors;

namespace DIME.Configurator;

public static class SourceConnectorFactory
{
    public static IConnector Create(string connectorType, Dictionary<object, object> section, Disruptor.Dsl.Disruptor<MessageBoxMessage> disruptor)
    {
        IConnector connector = null;
        
        switch (connectorType)
        {
            case "asccpc":
                connector = AscCpc.Source.Create(section, disruptor);
                break;
            case "ethernetip":
                connector = EthernetIp.Source.Create(section, disruptor);
                break;
            case "haasshdr":
                connector = HaasShdr.Source.Create(section, disruptor);
                break;
            case "httpserver":
                connector = HttpServer.Source.Create(section, disruptor);
                break;
            case "modbustcp":
                connector = ModbusTcp.Source.Create(section, disruptor);
                break;
            case "mqtt":
                connector = Mqtt.Source.Create(section, disruptor);
                break;
            case "mssql":
                connector = MsSql.Source.Create(section, disruptor);
                break;
            case "mtconnectagent":
                connector = MtConnectAgent.Source.Create(section, disruptor);
                break;
            case "opcua":
                connector = OpcUa.Source.Create(section, disruptor);
                break;
            case "postgres":
                connector = Postgres.Source.Create(section, disruptor);
                break;
            case "redis":
                connector = Redis.Source.Create(section, disruptor);
                break;
            case "script":
                connector = Script.Source.Create(section, disruptor);
                break;
            case "smartpac":
                connector = SmartPac.Source.Create(section, disruptor);
                break;
            case "snmp":
                connector = Snmp.Source.Create(section, disruptor);
                break;
            case "timebasews":
                connector = TimebaseWs.Source.Create(section, disruptor);
                break;
            default:
                break;
        }
        
        return connector;
    }
}