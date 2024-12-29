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
        // when Outbox was a list, it was necessary to manage concurrent access
        // even with concurrent access, the sink could clear the outbox and miss events
        while (Connector.IsWriting)
        {
            //System.Console.WriteLine("SinkMessageHandler /////////////////////////////////////////////////////////// SPIN");
            Thread.Sleep(10);
        }

        Connector.Outbox.Add(data);
    }
}