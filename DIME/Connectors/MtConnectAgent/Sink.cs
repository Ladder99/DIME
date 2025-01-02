using DIME.Configuration.MtConnectAgent;
using DIME.ConnectorSupport.MtConnect.DeviceBuilder;
using MTConnect.Applications;
using MTConnect.Configurations;
using MTConnect.Devices;

namespace DIME.Connectors.MtConnectAgent;

public class Sink: SinkConnector<ConnectorConfiguration, ConnectorItem>
{
    private MTConnectAgentApplication _client = null;
    
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
        _client.StartAgent(new AgentApplicationConfiguration() { 
            Modules = new []
            {
                new Dictionary<object, object> ()
                {
                    {"http-server", new Dictionary<object, object>()
                    {
                        {"port", Configuration.Port},
                    }}
                }
            }
        });
        
        return true;
    }

    protected override bool WriteImplementation()
    {
        foreach (var message in Outbox)
        {
            if (message.ConnectorItemRef is not null &&
                message.ConnectorItemRef.SinkMeta is not null &&
                message.ConnectorItemRef.SinkMeta.ContainsKey("mtconnect"))
            {
                var devices = _client.Agent.GetDevices().ToDictionary(o => o.Name, o => (Device)o);
                
                var (wasModified, device, dataItem) = 
                    Builder.Build(
                        devices, 
                        message.ConnectorItemRef.SinkMeta["mtconnect"].ToString(), 
                        message.Path);

                if (wasModified)
                {
                    _client.Agent.AddDevice(device);
                }
                
                _client.Agent.AddObservation(device.Uuid, dataItem.Id, Configuration.UseSinkTransform ? TransformAndSerializeMessage(message) : message.Data, message.Timestamp);
            }
        }
        
        return true;
    }

    protected override bool DisconnectImplementation()
    {
        _client.StopAgent();
        return true;
    }
    
    protected override bool DeinitializeImplementation()
    {
        return true;
    }
}