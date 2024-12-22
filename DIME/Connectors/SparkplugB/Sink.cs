using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using DIME.Configuration.SparkplugB;
using IronPython.Modules;
using Newtonsoft.Json;
using SparkplugNet.Core;
using SparkplugNet.Core.Enumerations;
using SparkplugNet.Core.Node;
using SparkplugNet.VersionB;
using SparkplugNet.VersionB.Data;

namespace DIME.Connectors.SparkplugB;

public class Sink: SinkConnector<ConnectorConfiguration, ConnectorItem>
{
    private SparkplugNode _client = null;
    private List<Metric> _nodeMetrics = null;
    private SparkplugNodeOptions _nodeOptions = null;
    
    private long _nodeConnectedTime = 0;
    private bool _deviceHasBirthed = false;
    private bool _deviceRebirthTrigger = false;

    private Dictionary<string, MessageBoxMessage> _messages = null;
    
    public Sink(ConnectorConfiguration configuration, Disruptor.Dsl.Disruptor<MessageBoxMessage> disruptor) : base(configuration, disruptor)
    {
    }

    protected override bool InitializeImplementation()
    {
        Properties.SetProperty("client_id", Guid.NewGuid().ToString());
        _messages = new Dictionary<string, MessageBoxMessage>();
        return true;
    }

    protected override bool CreateImplementation()
    {
        SparkplugGlobals.UseStrictIdentifierChecking = false;
        
        _nodeMetrics = new List<Metric>
        {
            new("IpAddress", DataType.String, string.Join(';', 
                NetworkInterface.GetAllNetworkInterfaces()
                    .Where(x => x.OperationalStatus == OperationalStatus.Up)
                    .SelectMany(x => x.GetIPProperties().UnicastAddresses)
                    .Where(x => x.Address.AddressFamily == AddressFamily.InterNetwork)
                    .Select(x => x.Address.ToString())
                    .ToList()))
        };
        
        _client = new SparkplugNode(_nodeMetrics, SparkplugSpecificationVersion.Version22);
        
        _nodeOptions = new SparkplugNodeOptions(
            brokerAddress: Configuration.Address,
            port: Configuration.Port,
            userName: Configuration.Username,
            clientId: Properties.GetProperty<string>("client_id"),
            password: Configuration.Password,
            scadaHostIdentifier: Configuration.HostId,
            groupIdentifier: Configuration.GroupId,
            edgeNodeIdentifier: Configuration.NodeId,
            reconnectInterval: TimeSpan.FromSeconds(Configuration.ReconnectIntervalMs/1000)
        );

        _client.Connected += async args =>
        {
            Logger.Debug($"[{Configuration.Name}] Connected.");
            _deviceHasBirthed = false;
            _nodeConnectedTime = DateTime.Now.ToEpochMilliseconds();
            await Task.FromResult(0);
        };
        
        _client.Disconnected += async args =>
        {
            Logger.Debug($"[{Configuration.Name}] Disconnected.");
            IsConnected = false;
            await Task.FromResult(0);
        };
        
        _client.NodeCommandReceived += async args =>
        {
            Logger.Debug($"[{Configuration.Name}] Node command received.");
            await Task.FromResult(0);
        };
        
        _client.DeviceCommandReceived += async args =>
        {
            Logger.Debug($"[{Configuration.Name}] Device command received.");
            await Task.FromResult(0);
        };
        
        _client.StatusMessageReceived += async args =>
        {
            Logger.Debug($"[{Configuration.Name}] Status message received.");
            await Task.FromResult(0);
        };
        
        _client.DeviceBirthPublishing += async args =>
        {
            Logger.Debug($"[{Configuration.Name}] Device birth publishing.");
            await Task.FromResult(0);
        };
        
        _client.DeviceDeathPublishing += async args =>
        {
            Logger.Debug($"[{Configuration.Name}] Device death publishing.");
            await Task.FromResult(0);
        };

        try
        {
            _client.Start(_nodeOptions).GetAwaiter().GetResult();
        }
        catch (Exception e)
        {
            Logger.Warn(e, $"[{Configuration.Name}] Immediate node start failed.");
        }
        
        return true;
    }

    protected override bool ConnectImplementation()
    {
        if (_client.IsConnected)
        {
            _client.PublishMetrics(_nodeMetrics);
        }
        
        return _client.IsConnected;
    }
    
    protected override bool WriteImplementation()
    {
        var samples = new List<Metric>();
        
        foreach (var message in Outbox)
        {
            // TODO: DATA CORRUPTION
            var tempMessage = new MessageBoxMessage()
            {
                Path = message.Path,
                Data = Configuration.UseSinkTransform ? TransformAndSerializeMessage(message) : message.Data,
                Timestamp = message.Timestamp,
                ConnectorItemRef = message.ConnectorItemRef
            };
            
            if (!_messages.ContainsKey(tempMessage.Path))
            {
                //Console.WriteLine(tempMessage.Path);
                // must rebirth due to new observation
                _deviceRebirthTrigger = true;
            }
            
            _messages[tempMessage.Path] = tempMessage;

            samples.Add(MakeMetric(tempMessage.Path, tempMessage.Data));
        }

        if (_deviceHasBirthed)
        {
            // new observation was found, need to rebirth
            if (_deviceRebirthTrigger)
            {
                //Console.WriteLine("REBIRTH triggered.");
                _client.PublishDeviceBirthMessage(MakeMetricListFromMessages(), Configuration.DeviceId);
                _deviceRebirthTrigger = false;
            }
            // continue publishing data for known metrics
            else
            {
                _client.PublishDeviceData(samples, Configuration.DeviceId);
            }
        }
        else
        {
            // device has connected or reconnected, stall birth to collect as many known metrics
            if (DateTime.Now.ToEpochMilliseconds() - _nodeConnectedTime > Configuration.BirthDelayMs)
            {
                //Console.WriteLine("BIRTH triggered.");
                _client.PublishDeviceBirthMessage(MakeMetricListFromMessages(), Configuration.DeviceId);
                _deviceHasBirthed = true;
                _deviceRebirthTrigger = false;
            }
        }
        
        return true;
    }

    protected override bool DisconnectImplementation()
    {
        return true;
    }
    
    protected override bool DeinitializeImplementation()
    {
        _client.Stop();
        return true;
    }
    
    private List<Metric> MakeMetricListFromMessages()
    {
        var metrics = new List<Metric>();

        foreach (var message in _messages)
        {
            metrics.Add(MakeMetric(message.Key, message.Value.Data));
        }
        
        return metrics;
    }
    
    private Metric MakeMetric(string name, object value)
    {
        if (value is null)
        {
            value = string.Empty;
        }
        
        name = Regex.Replace(name, @"[^0-9a-zA-Z_./]+", "");
        
        switch (Type.GetTypeCode(value.GetType()))
        {
            case TypeCode.Byte:
                return new Metric(name, DataType.Int8, Convert.ToUInt16(value), DateTimeOffset.Now);
            case TypeCode.Int16:
            case TypeCode.Int32:
            case TypeCode.Int64:
            case TypeCode.Double:
                return new Metric(name, DataType.Double, Convert.ToDouble(value), DateTimeOffset.Now);
            case TypeCode.Boolean:
                return new Metric(name, DataType.Boolean, Convert.ToBoolean(value), DateTimeOffset.Now);
            case TypeCode.String:
                return new Metric(name, DataType.String, Convert.ToString(value) ?? string.Empty, DateTimeOffset.Now);
            default:
                Logger.Debug($"[{Configuration.Name}] '{name}'({value.GetType().FullName}) converted to string metric.");
                return new Metric(name, DataType.String, JsonConvert.SerializeObject(value), DateTimeOffset.Now);
        }
    }
}