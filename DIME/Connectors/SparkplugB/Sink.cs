using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using DIME.Configuration.SparkplugB;
using IronPython.Modules;
using Newtonsoft.Json;
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
    
    private long _connectorStartTime = 0;
    private long _birthStallTime = 10000;
    private bool _deviceHasBirthed = false;
    private bool _deviceRebirthTrigger = false;

    private Dictionary<string, MessageBoxMessage> _messages = null;
    
    public Sink(ConnectorConfiguration configuration, Disruptor.Dsl.Disruptor<MessageBoxMessage> disruptor) : base(configuration, disruptor)
    {
    }

    protected override bool InitializeImplementation()
    {
        Properties.SetProperty("client_id", Guid.NewGuid().ToString());
        _connectorStartTime = DateTime.Now.ToEpochMilliseconds();
        _messages = new Dictionary<string, MessageBoxMessage>();
        return true;
    }

    protected override bool CreateImplementation()
    {
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
        
        _client = new SparkplugNode(_nodeMetrics, SparkplugSpecificationVersion.Version30);
        
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
            await Task.FromResult(0);
        };
        
        _client.Disconnected += async args =>
        {
            Logger.Debug($"[{Configuration.Name}] Disconnected.");
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
        
        
        return true;
    }

    protected override bool ConnectImplementation()
    {
        try
        {
            _client.Start(_nodeOptions);
            _client.PublishMetrics(_nodeMetrics);
        }
        catch (Exception e)
        {
            return false;
        }
        
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

    protected override bool WriteImplementation()
    {
        var samples = new List<Metric>();
        
        foreach (var message in Outbox)
        {
            // TODO: DATA CORRUPTION
            var tempMessage = new MessageBoxMessage()
            {
                Path = message.Path,
                Data = message.Data,
                Timestamp = message.Timestamp,
                ConnectorItemRef = message.ConnectorItemRef
            };
            
            if (!_messages.ContainsKey(tempMessage.Path))
            {
                _deviceRebirthTrigger = true;
            }
            
            _messages[tempMessage.Path] = tempMessage;

            samples.Add(MakeMetric(tempMessage.Path, tempMessage.Data));
        }

        if (_deviceHasBirthed)
        {
            if (_deviceRebirthTrigger)
            {
                //_client.PublishDeviceDeathMessage(Configuration.DeviceId);
                _client.PublishDeviceBirthMessage(MakeMetricListFromMessages(), Configuration.DeviceId);
                _deviceRebirthTrigger = false;
            }
            else
            {
                _client.PublishDeviceData(samples, Configuration.DeviceId);
            }
        }
        else
        {
            if (DateTime.Now.ToEpochMilliseconds() - _connectorStartTime > _birthStallTime)
            {
                _client.PublishDeviceBirthMessage(MakeMetricListFromMessages(), Configuration.DeviceId);
                _deviceHasBirthed = true;
            }
        }
        
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