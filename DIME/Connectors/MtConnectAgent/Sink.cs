using DIME.Configuration.MtConnectAgent;
using DIME.ConnectorSupport.MtConnect.DeviceBuilder;
using MTConnect.Agents;
using MTConnect.Applications;
using MTConnect.Configurations;
using MTConnect.Devices;
using MTConnect.Devices.Components;
using MTConnect.Devices.DataItems;
using MTConnect.Observations.Events;

namespace DIME.Connectors.MtConnectAgent;

public class Sink: SinkConnector<ConnectorConfiguration, ConnectorItem>
{
    private MTConnectAgentApplication _client = null;
    private Module _module = null;
    
    public Sink(ConnectorConfiguration configuration, Disruptor.Dsl.Disruptor<MessageBoxMessage> disruptor) : base(configuration, disruptor)
    {
    }

    protected override bool InitializeImplementation()
    {
        return true;
    }

    protected override bool CreateImplementation()
    {
        _client = new MTConnectAgentApplication();
        return true;
    }

    protected override bool ConnectImplementation()
    {
        //_client.StartAgent(new AgentApplicationConfiguration());
        _client.Run(["run", "MtConnectAgentSink.yaml"]);
        _module = new Module(_client.Agent, this);
        _module.Start();
        IsConnected = true;
        return true;
    }

    protected override bool WriteImplementation()
    {
        return true;
    }

    protected override bool DisconnectImplementation()
    {
        _module.Stop();
        _client.StopAgent();
        IsConnected = false;
        return true;
    }
    
    protected override bool DeinitializeImplementation()
    {
        return true;
    }
}

public class Module : MTConnectInputAgentModule
{
    public const string ConfigurationTypeId = "dime-sink";
    Sink _connector = null;
    
    public Module(IMTConnectAgentBroker agent, Sink connector) : base(agent)
    {
        _connector = connector;
        this.StartBeforeLoad(false);
    }
    
    protected override IDevice OnAddDevice()
    {
        var device = new Device();
        device.Uuid = _connector.Configuration.DeviceUuid;
        device.Id = _connector.Configuration.DeviceId;
        device.Name = _connector.Configuration.DeviceName;
        device.Description = new Description()
        {
            //Manufacturer = _connector.Configuration.DeviceManufacturer,
            //Model = _connector.Configuration.DeviceModel,
            //SerialNumber = _connector.Configuration.DeviceSerialNumber
        };
        
        return device;
    }
    
    protected override void OnRead()
    {
        var devices = new Dictionary<string, Device>();
        devices[Device.Name] = (Device)Device;
        
        foreach (var message in _connector.Outbox)
        {
            if (message.ConnectorItemRef is not null &&
                message.ConnectorItemRef.Meta is not null &&
                message.ConnectorItemRef.Meta.ContainsKey("mtconnect"))
            {
                var (wasModified, device, dataItem) = 
                    Builder.Build(
                        devices, 
                        message.ConnectorItemRef.Meta["mtconnect"].ToString(), 
                        message.Path);

                if (wasModified)
                {
                    Agent.AddDevice(device);
                }

                AddValueObservation(dataItem, message.Data, message.Timestamp);
            }
        }
        
        _connector.Outbox.Clear();
    }
}