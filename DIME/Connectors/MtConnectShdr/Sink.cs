using DIME.Configuration.MtConnectShdr;
using DIME.ConnectorSupport.MtConnect.DeviceBuilder;
using MTConnect.Adapters;
using MTConnect.Devices;
using MTConnect.Observations;
using MTConnect.Shdr;

namespace DIME.Connectors.MtConnectShdr;

public class Sink: SinkConnector<ConnectorConfiguration, Configuration.ConnectorItem>
{
    private ShdrQueueAdapter _client = null;
    private Dictionary<string, Device> _devices = null;
    private bool _writeFile = false;
    
    public Sink(ConnectorConfiguration configuration, Disruptor.Dsl.Disruptor<MessageBoxMessage> disruptor) : base(configuration, disruptor)
    {
    }

    protected override bool InitializeImplementation()
    {
        return true;
    }

    protected override bool CreateImplementation()
    {
        _devices = new Dictionary<string, Device>();
        
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
                message.ConnectorItemRef.SinkMeta is not null &&
                message.ConnectorItemRef.SinkMeta.ContainsKey("mtconnect"))
            {
                var (wasModified, device, dataItem) = 
                    Builder.Build(
                        _devices, 
                        message.ConnectorItemRef.SinkMeta["mtconnect"].ToString(),
                        message.Path);
                
                if (wasModified) _writeFile = true;

                switch (dataItem.Category)
                {
                    case DataItemCategory.SAMPLE:
                        _client.AddDataItem(
                            new ShdrDataItem(
                                message.Path, 
                                message.Data, 
                                message.Timestamp));
                        break;
                    
                    case DataItemCategory.CONDITION:
                        _client.AddCondition(
                            new ShdrCondition(
                                message.Path, 
                                (ConditionLevel)Enum.Parse(typeof(ConditionLevel), message.Data.ToString().ToUpper()), 
                                message.Timestamp));
                        break;
                    
                    case DataItemCategory.EVENT:
                        switch (dataItem.Type.ToUpper())
                        {
                            case "MESSAGE":
                                _client.AddMessage(
                                    new ShdrMessage(
                                        message.Path,
                                        message.Data.ToString(),
                                        message.Timestamp));
                                break;
                            
                            default:
                                _client.AddDataItem(
                                    new ShdrDataItem(
                                        message.Path, 
                                        message.Data, 
                                        message.Timestamp));
                                break;
                        }
                        break;
                }
            }
            else
            {
                _client.AddDataItem(new ShdrDataItem(message.Path, TransformAndSerializeMessage(message), message.Timestamp));
            }
        }

        _client.SendBuffer();

        return true;
    }

    public override bool AfterUpdate()
    {
        if (_writeFile)
        {
            Directory.CreateDirectory(Configuration.OutputFolder);
        
            foreach (var device in _devices)
            {
                var xmlBytes = MTConnect.Devices.Xml.XmlDevice.ToXml(device.Value);
                var xmlString = System.Text.Encoding.UTF8.GetString(xmlBytes);
                File.WriteAllText($"{Configuration.OutputFolder}/device-{device.Key}.xml", xmlString);
            }
            
            _writeFile = false;
        }
        
        
        return base.AfterUpdate();
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