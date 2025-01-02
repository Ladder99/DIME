using System.Net;
using System.Text;
using Apache.NMS;
using Apache.NMS.ActiveMQ.Commands;
using Apache.NMS.Util;
using DIME.Configuration.ActiveMq;

namespace DIME.Connectors.ActiveMq;

public class Source: QueuingSourceConnector<ConnectorConfiguration, ConnectorItem>
{
    private IConnection _connection = null;
    private ISession _session = null;
    private List<IMessageConsumer> _consumers = null;

    public Source(ConnectorConfiguration configuration, Disruptor.Dsl.Disruptor<MessageBoxMessage> disruptor) : base(configuration, disruptor)
    {
        
    }

    protected override bool InitializeImplementation()
    {
        return true;
    }

    protected override bool CreateImplementation()
    {
        var factory = new NMSConnectionFactory(new Uri(Configuration.Address));
        _connection = factory.CreateConnection(Configuration.Username, Configuration.Password);
        return true;
    }

    protected override bool ConnectImplementation()
    {
        _connection.Start();
        _session = _connection.CreateSession(AcknowledgementMode.AutoAcknowledge);
        _consumers = new List<IMessageConsumer>();
        foreach (var item in Configuration.Items)
        {
            var consumer = _session.CreateConsumer(SessionUtil.GetDestination(_session, item.Address));
            consumer.Listener += ConsumerOnListener;
            _consumers.Add(consumer);
        }
        
        return _connection.IsStarted;
    }

    protected override bool DisconnectImplementation()
    {
        foreach (var consumer in _consumers)
        {
            consumer.Listener -= ConsumerOnListener;
            consumer.Close();
        }
        _consumers.Clear();
        _session.Close();
        _connection.Close();
        _connection.Stop();
        return true;
    }
    
    protected override bool DeinitializeImplementation()
    {
        return true;
    }

    private void ConsumerOnListener(IMessage message)
    {
        ActiveMQMessage activeMQMessage = message as ActiveMQMessage;
        string content = Encoding.UTF8.GetString(activeMQMessage.Content);
        string destination = activeMQMessage.Destination.ToString();
        AddToIncomingBuffer(destination, content, message.NMSTimestamp.ToEpochMilliseconds());
    }
}