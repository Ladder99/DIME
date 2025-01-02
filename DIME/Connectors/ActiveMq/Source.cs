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
        _consumers = new List<IMessageConsumer>();
        return true;
    }

    protected override bool CreateImplementation()
    {
        return true;
    }

    protected override bool ConnectImplementation()
    {
        var factory = new NMSConnectionFactory(new Uri(Configuration.Address));
        _connection = factory.CreateConnection(Configuration.Username, Configuration.Password);
        _connection.ExceptionListener += ConnectionOnExceptionListener;
        _connection.Start();
        _session = _connection.CreateSession(AcknowledgementMode.AutoAcknowledge);
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
            consumer.Dispose();
        }
        _consumers.Clear();
        _session.Close();
        _session.Dispose();
        _connection.ExceptionListener -= ConnectionOnExceptionListener;
        _connection.Close();
        _connection.Stop();
        _connection.Dispose();
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
    
    private void ConnectionOnExceptionListener(Exception exception)
    {
        IsConnected = false;
    }
}