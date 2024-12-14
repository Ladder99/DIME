using System.Collections.Concurrent;
using DIME.Configuration;

namespace DIME.Connectors;

public abstract class SinkConnector<TConfig, TItem> : Connector<TConfig, TItem>, ISinkConnector
    where TConfig : ConnectorConfiguration<TItem>
    where TItem : ConnectorItem
{
    public ConcurrentBag<MessageBoxMessage> Outbox { get; private set; } = new ConcurrentBag<MessageBoxMessage>();
    public event Action<ConcurrentBag<MessageBoxMessage>> OnOutboxReady;
    public event Action<ConcurrentBag<MessageBoxMessage>, bool> OnOutboxSent;
    
    public bool IsWriting { get; protected set; }
    
    protected SinkConnector(TConfig configuration, Disruptor.Dsl.Disruptor<MessageBoxMessage> disruptor): base(configuration, disruptor)
    {
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
                OnOutboxReady?.Invoke(Outbox);
                
                EntireReadLoopStopwatch.Start();
                
                //TODO: implement inclusion filter
                
                Outbox = new ConcurrentBag<MessageBoxMessage>(Outbox
                    .Where(x => !Configuration.ExcludeFilter.Contains(x.ConnectorItemRef.Configuration.Name)));
                
                result = WriteImplementation();
                
                EntireReadLoopStopwatch.Stop();
                
                OnOutboxSent?.Invoke(Outbox, result);

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
        
        Logger.Debug($"[{Configuration.Name}] Loop Perf. " +
                    $"DeviceRead: {ReadFromDeviceSumStopwatch.ElapsedMilliseconds}ms, " +
                    $"ExecuteScript: {ExecuteScriptSumStopwatch.ElapsedMilliseconds}ms, " +
                    $"EntireLoop: {EntireReadLoopStopwatch.ElapsedMilliseconds}ms");
        
        base.InvokeOnLoopPerf(
            ReadFromDeviceSumStopwatch.ElapsedMilliseconds,
            ExecuteScriptSumStopwatch.ElapsedMilliseconds,
            EntireReadLoopStopwatch.ElapsedMilliseconds);
        
        EntireReadLoopStopwatch.Reset();

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