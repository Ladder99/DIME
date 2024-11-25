using System.Collections.Concurrent;
using IDS.Transporter.Configuration;

namespace IDS.Transporter.Connectors;

public abstract class SinkConnector<TConfig, TItem> : Connector<TConfig, TItem>, ISinkConnector
    where TConfig : ConnectorConfiguration<TItem>
    where TItem : ConnectorItem
{
    public ConcurrentBag<BoxMessage> Outbox { get; set; }
    
    public SinkConnector(TConfig configuration, Disruptor.Dsl.Disruptor<BoxMessage> disruptor): base(configuration, disruptor)
    {
        Outbox = new ConcurrentBag<BoxMessage>();
    }
    
    protected virtual bool BeforeWrite()
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
            BeforeWrite();
            var result = WriteImplementation();
            AfterWrite();

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

    protected virtual bool AfterWrite()
    {
        Outbox.Clear();
        return true;
    }
}