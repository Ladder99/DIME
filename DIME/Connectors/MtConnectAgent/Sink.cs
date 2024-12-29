using DIME.Configuration.MtConnectAgent;
using DIME.ConnectorSupport.MtConnect.DeviceBuilder;
using Mono.Unix.Native;
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
        //_client.StartAgent(new AgentApplicationConfiguration() { });
        _client.Run(["run", "MtConnectAgentSink.yaml"], false);
        _module = new Module(_client.Agent, this);
        _module.Start();
        IsConnected = true;
        return true;
    }

    protected override bool WriteImplementation()
    {
        return true;
    }

    public override bool AfterUpdate()
    {
        Logger.Trace($"[{Configuration.Name}] SinkConnector:AfterUpdate::ENTER");
        
        // Do not clear outbox here.  That is the module's job.

        IsWriting = false;
        
        Logger.Trace($"[{Configuration.Name}] SinkConnector:AfterUpdate::EXIT");
        
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
    }
    
    protected override void OnRead()
    {
        _connector.IsWriting = true;
        
        foreach (var message in _connector.Outbox)
        {
            if (message.ConnectorItemRef is not null &&
                message.ConnectorItemRef.SinkMeta is not null &&
                message.ConnectorItemRef.SinkMeta.ContainsKey("mtconnect"))
            {
                var devices = Agent.GetDevices().ToDictionary(o => o.Name, o => (Device)o);
                
                var (wasModified, device, dataItem) = 
                    Builder.Build(
                        devices, 
                        message.ConnectorItemRef.SinkMeta["mtconnect"].ToString(), 
                        message.Path);

                //System.Console.WriteLine($"///////// {message.Path} / di={dataItem.Id} / diDevice={dataItem.Device.Uuid} / device={device.Uuid}");

                if (wasModified)
                {
                    Agent.AddDevice(device);
                }
                
                Agent.AddObservation(device.Uuid, dataItem.Id, _connector.Configuration.UseSinkTransform ? _connector.TransformAndSerializeMessage(message) : message.Data, message.Timestamp);
            }
        }
        
        _connector.Outbox.Clear();
        
        _connector.IsWriting = false;
    }
}