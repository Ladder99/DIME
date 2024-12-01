using System.Collections.Concurrent;
using IDS.Transporter.Configuration;

namespace IDS.Transporter.Connectors;

public abstract class SourceConnector<TConfig, TItem>: Connector<TConfig, TItem>, ISourceConnector
    where TConfig : ConnectorConfiguration<TItem>
    where TItem : ConnectorItem
{
    private bool? _wasConnected = null;
    private bool? _wasFaulted = null;
    
    protected LuaRunner ScriptRunner { get; set; }
    public ConcurrentBag<MessageBoxMessage> Inbox { get; set; }
    public ConcurrentBag<MessageBoxMessage> Samples { get; set; }
    public ConcurrentBag<MessageBoxMessage> Current { get; set; }
    
    public SourceConnector(TConfig configuration, Disruptor.Dsl.Disruptor<MessageBoxMessage> disruptor): base(configuration, disruptor)
    {
        ScriptRunner = new LuaRunner();
        Inbox = new ConcurrentBag<MessageBoxMessage>();
        Samples = new ConcurrentBag<MessageBoxMessage>();
        Current = new ConcurrentBag<MessageBoxMessage>();
    }

    public override bool Initialize(ConnectorRunner runner)
    {
        return base.Initialize(runner) && ScriptRunner.Initialize(this);
    }

    protected object ExecuteScript(object intermediateResult, string script)
    {
        ScriptRunner["result"] = intermediateResult;
        var scriptResult = ScriptRunner.DoString(script);
        return scriptResult.Length == 1 ? scriptResult[0] : scriptResult;
    }
    
    public override bool BeforeUpdate()
    {
        Samples.Clear();
        return true;
    }
    
    protected abstract bool ReadImplementation();

    public virtual bool Read()
    {
        FaultContext = FaultContextEnum.Read;
        
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
            var result = ReadImplementation();

            if (result)
            {
                ClearFault();
            }
            else
            {
                MarkFaulted(new Exception("Device implementation read failed."));
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
        FillInbox();
        PublishInbox();
        
        return true;
    }

    private void FillInbox()
    {
        Inbox.Clear();
        
        if (_wasConnected != IsConnected)
        {
            Samples.Add(new MessageBoxMessage()
            {
                Path = $"{Configuration.Name}/$SYSTEM/IsConnected",
                Data = IsConnected,
                Timestamp = DateTime.UtcNow.ToEpochMilliseconds(),
                ConnectorItemRef = new ConnectorItem()
                {
                    Configuration = Configuration
                }
            });
            
            Logger.Debug($"[{Configuration.Name}] Fill Inbox. Emit $SYSTEM/IsConnected = {IsConnected}");
            
            _wasConnected = IsConnected;
        }
        
        if (_wasFaulted != IsFaulted)
        {
            Samples.Add(new MessageBoxMessage()
            {
                Path = $"{Configuration.Name}/$SYSTEM/IsFaulted",
                Data = IsFaulted,
                Timestamp = DateTime.UtcNow.ToEpochMilliseconds(),
                ConnectorItemRef = new ConnectorItem()
                {
                    Configuration = Configuration
                }
            });
            
            Samples.Add(new MessageBoxMessage()
            {
                Path = $"{Configuration.Name}/$SYSTEM/Fault",
                Data = FaultReason,
                Timestamp = DateTime.UtcNow.ToEpochMilliseconds(),
                ConnectorItemRef = new ConnectorItem()
                {
                    Configuration = Configuration
                }
            });
            
            Logger.Debug($"[{Configuration.Name}] Fill Inbox. Emit $SYSTEM/IsFaulted = {IsFaulted}");
            Logger.Debug($"[{Configuration.Name}] Fill Inbox. Emit $SYSTEM/Fault = {FaultReason}");
            
            _wasFaulted = IsFaulted;
        }
        
        foreach (var sampleResponse in Samples)
        {
            MessageBoxMessage matchingCurrent = null;

            try
            {
                matchingCurrent = Current
                    .First(x => x.Path == sampleResponse.Path);
            }
            catch (InvalidOperationException e)
            {
            }

            // sample does not exist in current, it is a new sample
            if (matchingCurrent is null)
            {
                Logger.Trace($"[{sampleResponse.Path}] Fill Inbox. New sample added to inbox.");
                
                Inbox.Add(sampleResponse);
                Current.Add(sampleResponse);
            }
            // sample data is different, it is an updated sample
            else
            {
                var confRbe = Configuration.ReportByException;
                var itemRbe = sampleResponse.ConnectorItemRef == null || sampleResponse.ConnectorItemRef.ReportByException;

                if (!confRbe && itemRbe || confRbe && itemRbe)
                {
                    if (!matchingCurrent.Data.Equals(sampleResponse.Data))
                    {
                        Logger.Trace($"[{sampleResponse.Path}] Fill Inbox. RBE sample added to inbox. " +
                                     $"Current={matchingCurrent.Data}, Sample={sampleResponse.Data}");
                        
                        Inbox.Add(sampleResponse);
                    }
                    else
                    {
                        Logger.Trace($"[{sampleResponse.Path}] Fill Inbox. RBE sample dropped. " +
                                     $"Current={matchingCurrent.Data}, Sample={sampleResponse.Data}");
                    }
                }
                else
                {
                    Logger.Trace($"[{sampleResponse.Path}] Fill Inbox. Non-RBE sample added to inbox.");
                    
                    Inbox.Add(sampleResponse);
                }

                matchingCurrent.Data = sampleResponse.Data;
                matchingCurrent.Timestamp = sampleResponse.Timestamp;
            }
        }
    }

    private void PublishInbox()
    {
        if (Inbox.Count > 0)
        {
            using (var scope = Disruptor.RingBuffer.PublishEvents(Inbox.Count))
            {
                int index = 0;
                foreach (var response in Inbox)
                {
                    var data = scope.Event(index);
                    data.Data = response.Data;
                    data.Path = response.Path;
                    data.Timestamp = response.Timestamp;
                    data.ConnectorItemRef = response.ConnectorItemRef;
                    index++;
                }
            }
        }
    }
}