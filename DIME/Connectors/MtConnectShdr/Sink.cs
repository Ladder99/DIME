using DIME.Configuration.MtConnectShdr;
using MTConnect.Adapters;
using MTConnect.Shdr;

namespace DIME.Connectors.MtConnectShdr;

public class Sink: SinkConnector<ConnectorConfiguration, Configuration.ConnectorItem>
{
    private ShdrQueueAdapter _client = null;
    
    public Sink(ConnectorConfiguration configuration, Disruptor.Dsl.Disruptor<MessageBoxMessage> disruptor) : base(configuration, disruptor)
    {
    }

    protected override bool InitializeImplementation()
    {
        return true;
    }

    protected override bool CreateImplementation()
    {
        _client = new ShdrQueueAdapter(
            Configuration.DeviceKey,
            Configuration.Port,
            Configuration.HeartbeatMs);
        
        _client.FilterDuplicates = Configuration.FilterDuplicates;
        
        _client.AgentConnectionError += (sender, s) =>
        {
            Logger.Warn($"[{Configuration.Name}] MTC Agent connection error. {s}");
        };

        _client.AgentDisconnected += (sender, s) =>
        {
            Logger.Warn($"[{Configuration.Name}] MTC Agent disconnected error. {s}");
        };

        _client.AgentConnected += (sender, s) =>
        {
            Logger.Info($"[{Configuration.Name}] MTC Agent connected. {s}");
            _client.SendChanged();
        };

        _client.SendError += (sender, args) =>
        {
            Logger.Warn($"[{Configuration.Name}] MTC Agent send error. {args.Data}");
        };

        _client.LineSent += (sender, args) =>
        {
            Logger.Debug($"[{Configuration.Name}] MTC Agent line send. {args.Data}");
        };
        
        _client.PingReceived += (sender, s) =>
        {
            Logger.Debug($"[{Configuration.Name}] MTC Agent ping received. {s}");
        };

        _client.PongSent += (sender, s) =>
        {
            Logger.Debug($"[{Configuration.Name}] MTC Agent pong sent. {s}");
        };
        
        return true;
    }

    protected override bool ConnectImplementation()
    {
        _client.Start();
        return true;
    }

    protected override bool WriteImplementation()
    {
        foreach (var message in Outbox)
        {
            //Console.WriteLine($"{response.Path} = {response.Data}");

            if (message.ConnectorItemRef is not null && 
                message.ConnectorItemRef.Meta is not null &&
                message.ConnectorItemRef.Meta.ContainsKey("mtconnect"))
            {
                string mtconnectPath = message.ConnectorItemRef.Meta["mtconnect"].ToString();
                string mtconnectSource = message.Path;
            }
            
            _client.AddDataItem(new ShdrDataItem(message.Path, message.Data, message.Timestamp));
        }

        _client.SendBuffer();
        
        return true;
    }

    protected override bool DisconnectImplementation()
    {
        _client.Stop();
        return true;
    }
    
    protected override bool DeinitializeImplementation()
    {
        return true;
    }
}