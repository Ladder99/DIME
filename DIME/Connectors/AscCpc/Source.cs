using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using DIME.Configuration.AscCpc;
using MTConnect;
using NLog;

namespace DIME.Connectors.AscCpc;

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
        string host = Configuration.IpAddress;
        int timeout = 1000;

        var reply = pingSender.Send(host, timeout);
        return reply.Status == IPStatus.Success;
    }

    protected override object ReadFromDevice(ConnectorItem item)
    {
        object response = null;
        string prefix = "PathListGet:ReadValues:";
        
        using (TcpClient client = new TcpClient(Configuration.IpAddress, Configuration.Port))
        using (NetworkStream stream = client.GetStream())
        {
            byte[] data = Encoding.ASCII.GetBytes($"{prefix}{item.Address}\r\n");
            stream.Write(data, 0, data.Length);
            byte[] buffer = new byte[1024];
            int bytesRead = stream.Read(buffer, 0, buffer.Length);
            response = Encoding.ASCII
                .GetString(buffer, 0, bytesRead)
                .Replace(prefix, "");
        }

        return response;
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