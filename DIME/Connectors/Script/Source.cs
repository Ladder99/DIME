using DIME.Configuration.Script;

namespace DIME.Connectors.Script;

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

    protected override bool ReadImplementation()
    {
        return base.ReadImplementation();
    }

    protected override bool DisconnectImplementation()
    {
        return true;
    }
}