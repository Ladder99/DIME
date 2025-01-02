using DIME.Connectors;

namespace DIME;

public class SinkMessageHandler : Disruptor.IEventHandler<MessageBoxMessage>
{
    protected readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
    
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
            Logger.Debug($"[{Connector.Configuration.Name}] Connector is writing.... Spinning.");
            Thread.Sleep(5);
        }

        Connector.Outbox.Add(data);
    }
}