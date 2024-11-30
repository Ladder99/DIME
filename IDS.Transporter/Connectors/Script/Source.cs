using IDS.Transporter.Configuration.Script;

namespace IDS.Transporter.Connectors.Script;

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
        return true;
    }

    protected override object ReadFromDevice(ConnectorItem item)
    {
        return null;
    }
    
    protected override bool DisconnectImplementation()
    {
        return true;
    }
}