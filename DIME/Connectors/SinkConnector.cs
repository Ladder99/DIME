using System.Collections.Concurrent;
using DIME.Configuration;

namespace DIME.Connectors;

public abstract class SinkConnector<TConfig, TItem> : Connector<TConfig, TItem>, ISinkConnector
    where TConfig : ConnectorConfiguration<TItem>
    where TItem : ConnectorItem
{
    public ConcurrentBag<MessageBoxMessage> Outbox { get; set; }
    public bool IsWriting { get; protected set; }
    
    public SinkConnector(TConfig configuration, Disruptor.Dsl.Disruptor<MessageBoxMessage> disruptor): base(configuration, disruptor)
    {
        Outbox = new ConcurrentBag<MessageBoxMessage>();
        IsWriting = false;
        
        Logger.Trace($"[{Configuration.Name}] SinkConnector:.ctor");
    }
    
    public override bool BeforeUpdate()
    {
        Logger.Trace($"[{Configuration.Name}] SinkConnector:BeforeUpdate::ENTER");
        
        IsWriting = true;
        
        Logger.Trace($"[{Configuration.Name}] SinkConnector:BeforeUpdate::EXIT");
        
        return true;
    }
    
    protected abstract bool WriteImplementation();
    
    public virtual bool Write()
    {
        Logger.Trace($"[{Configuration.Name}] SinkConnector:Write::ENTER");
        
        FaultContext = FaultContextEnum.Write;

        bool result = false;
        
        if (!IsInitialized)
        {
            MarkFaulted(new Exception("Device not initialized."));
            result = false;
        }
        else if (!IsCreated)
        {
            MarkFaulted(new Exception("Device not created."));
            result = false;
        }
        else if (!IsConnected)
        {
            MarkFaulted(new Exception("Device not connected."));
            result = false;
        }
        else
        {
            try
            {
                result = WriteImplementation();

                if (result)
                {
                    ClearFault();
                }
                else
                {
                    MarkFaulted(new Exception("Device implementation write failed."));
                }
            }
            catch (Exception e)
            {
                MarkFaulted(e);
                Disconnect();
                result = false;
            }
        }
        
        Logger.Trace($"[{Configuration.Name}] SinkConnector:Write::EXIT");

        return result;
    }

    public override bool AfterUpdate()
    {
        Logger.Trace($"[{Configuration.Name}] SinkConnector:AfterUpdate::ENTER");
        
        Outbox.Clear();

        IsWriting = false;
        
        Logger.Trace($"[{Configuration.Name}] SinkConnector:AfterUpdate::EXIT");
        
        return true;
    }
}