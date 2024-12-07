using System.Net.NetworkInformation;
using System.Net.Sockets;
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
    private bool _deviceHasBirthed = false;
    
    public Sink(ConnectorConfiguration configuration, Disruptor.Dsl.Disruptor<MessageBoxMessage> disruptor) : base(configuration, disruptor)
    {
    }

    protected override bool InitializeImplementation()
    {
        Properties.SetProperty("client_id", Guid.NewGuid().ToString());
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

    private Metric MakeMetric(string name, object value)
    {
        switch (Type.GetTypeCode(value.GetType()))
        {
            case TypeCode.Byte:
                return new Metric(name, DataType.Int8, Convert.ToUInt16(value));
            case TypeCode.Int16:
            case TypeCode.Int32:
            case TypeCode.Int64:
            case TypeCode.Double:
                return new Metric(name, DataType.Double, Convert.ToDouble(value));
            case TypeCode.Boolean:
                return new Metric(name, DataType.Boolean, Convert.ToBoolean(value));
            case TypeCode.String:
                return new Metric(name, DataType.String, Convert.ToString(value) ?? string.Empty);
            default:
                Logger.Debug($"[{Configuration.Name}] '{name}'({value.GetType().FullName}) converted to string metric.");
                return new Metric(name, DataType.String, JsonConvert.SerializeObject(value));
        }
    }

    protected override bool WriteImplementation()
    {
        List<Metric> metrics = new List<Metric>();
        
        foreach (var message in Outbox)
        {
            if (message.Data is not null)
            {
                Metric metric = MakeMetric(message.Path, message.Data);
                metrics.Add(metric);
            }
        }

        _client.PublishDeviceBirthMessage(metrics, Configuration.DeviceId);
        
        /*
        if (!_deviceHasBirthed)
        {
            _client.PublishDeviceBirthMessage(metrics, Configuration.DeviceId);
            _deviceHasBirthed = true;
        }
        else
        {
            try
            {
                _client.PublishDeviceData(metrics, Configuration.DeviceId);
            }
            catch (Exception e)
            {
                
            }
            
        }
        */

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