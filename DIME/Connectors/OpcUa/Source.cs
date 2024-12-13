using DIME.Configuration.OpcUa;

namespace DIME.Connectors.OpcUa;

public class Source: PollingSourceConnector<ConnectorConfiguration, ConnectorItem>
{
    private OpcUaClient _client = null;

    public Source(ConnectorConfiguration configuration, Disruptor.Dsl.Disruptor<MessageBoxMessage> disruptor) : base(configuration, disruptor)
    {
    }

    protected override bool InitializeImplementation()
    { 
        return true;
    }
    
    protected override bool CreateImplementation()
    {
        _client = new OpcUaClient(
            Configuration.Address,
            Configuration.Port,
            Configuration.TimeoutMs,
            Configuration.UseAnonymousUser,
            Configuration.Username,
            Configuration.Password);
        return true;
    }

    protected override bool ConnectImplementation()
    {
        return _client.Connect();
    }

    protected override object ReadFromDevice(ConnectorItem item)
    {
        var result = _client.Read(item.Namespace, item.Address);

        return result.Value;
    }

    protected override bool DisconnectImplementation()
    {
        return _client.Disconnect();
    }
    
    protected override bool DeinitializeImplementation()
    {
        return true;
    }
}