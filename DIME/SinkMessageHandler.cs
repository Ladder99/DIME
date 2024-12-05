using DIME.Connectors;

namespace DIME;

public class SinkMessageHandler : Disruptor.IEventHandler<MessageBoxMessage>
{
    ISinkConnector Connector;
    
    public SinkMessageHandler(ISinkConnector connector)
    {
        Connector = connector;
    }
    
    public void OnEvent(MessageBoxMessage data, long sequence, bool endOfBatch)
    {
        while (Connector.IsWriting)
        { 
            Thread.Sleep(10);
        }
        
        Connector.Outbox.Add(data);
    }
}