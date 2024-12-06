using System.Net;
using System.Net.NetworkInformation;
using DIME.Configuration.Snmp;
using Lextm.SharpSnmpLib;
using Lextm.SharpSnmpLib.Messaging;
using NLog;

namespace DIME.Connectors.Snmp;

public class Source: PollingSourceConnector<ConnectorConfiguration, ConnectorItem>
{
    public Source(ConnectorConfiguration configuration, Disruptor.Dsl.Disruptor<MessageBoxMessage> disruptor) : base(configuration, disruptor)
    {
    }

    protected override bool InitializeImplementation()
    {
        return true;
    }

    protected override bool CreateImplementation()
    {
        return true;
    }

    protected override bool ConnectImplementation()
    {
        var pingSender = new Ping();
        string host = Configuration.Address;
        int timeout = 1000;

        var reply = pingSender.Send(host, timeout);
        return reply.Status == IPStatus.Success;
    }

    protected override object ReadFromDevice(ConnectorItem item)
    {
        GetRequestMessage message = new GetRequestMessage(
            0,
            VersionCode.V2,
            new OctetString(Configuration.Community),
            new List<Variable> { new Variable(new ObjectIdentifier(item.Address)) }
        );

        ISnmpMessage response = message.GetResponse(
            Configuration.TimeoutMs, 
            new IPEndPoint(IPAddress.Parse(Configuration.Address), Configuration.Port));

        if (response.Pdu().ErrorStatus.ToInt32() != 0)
        {
            throw ErrorException.Create("Error in response", IPAddress.Parse(Configuration.Address), response);
        }

        var result = response.Pdu().Variables;

        return result[0].Data.ToString();
    }
    
    protected override bool DisconnectImplementation()
    {
        return true;
    }
    
    protected override bool DeinitializeImplementation()
    {
        return true;
    }
}