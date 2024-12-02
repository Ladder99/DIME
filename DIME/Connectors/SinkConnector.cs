using System.Collections.Concurrent;
using DIME.Configuration;

namespace DIME.Connectors;

public abstract class SinkConnector<TConfig, TItem> : Connector<TConfig, TItem>, ISinkConnector
    where TConfig : ConnectorConfiguration<TItem>
    where TItem : ConnectorItem
{
    public ConcurrentBag<MessageBoxMessage> Outbox { get; set; }
    
    public SinkConnector(TConfig configuration, Disruptor.Dsl.Disruptor<MessageBoxMessage> disruptor): base(configuration, disruptor)
    {
        Outbox = new ConcurrentBag<MessageBoxMessage>();
    }
    
    public override bool BeforeUpdate()
    {
        return true;
    }
    
    protected abstract bool WriteImplementation();
    
    public virtual bool Write()
    {
        FaultContext = FaultContextEnum.Write;
        
        if (!IsInitialized)
        {
            MarkFaulted(new Exception("Device not initialized."));
            return false;
        }

        if (!IsCreated)
        {
            MarkFaulted(new Exception("Device not created."));
            return false;
        }

        if (!IsConnected)
        {
            MarkFaulted(new Exception("Device not connected."));
            return false;
        }

        try
        {
            var result = WriteImplementation();

            if (result)
            {
                ClearFault();
            }
            else
            {
                MarkFaulted(new Exception("Device implementation write failed."));
            }

            return result;
        }
        catch (Exception e)
        {
            MarkFaulted(e);
            Disconnect();
            return false;
        }
    }

    public override bool AfterUpdate()
    {
        Outbox.Clear();
        return true;
    }
}