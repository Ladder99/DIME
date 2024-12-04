using System.Collections.Concurrent;
using DIME.Configuration;
using Newtonsoft.Json;

namespace DIME.Connectors;

public abstract class SourceConnector<TConfig, TItem>: Connector<TConfig, TItem>, ISourceConnector
    where TConfig : ConnectorConfiguration<TItem>
    where TItem : ConnectorItem
{
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
        var result = base.Initialize(runner) && ScriptRunner.Initialize(this);
        if (!string.IsNullOrEmpty(Configuration.InitScript))
        {
            ExecuteScript(Configuration.InitScript);
        }
        return result;
    }

    protected object ExecuteScript(string script)
    {
        var scriptResult = ScriptRunner.DoString(script);
        return scriptResult.Length == 1 ? scriptResult[0] : scriptResult;
    }
    
    protected object ExecuteScript(object intermediateResult, ConnectorItem item)
    {
        ScriptRunner["result"] = intermediateResult;
        var scriptResult = ScriptRunner.DoString(item);
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
        AddSystemSamples();
        FillInbox();
        PublishInbox();
        Inbox.Clear();
        
        return true;
    }
    
    public override bool Deinitialize()
    {
        var result = base.Deinitialize();
        if (!string.IsNullOrEmpty(Configuration.DeinitScript))
        {
            ExecuteScript(Configuration.DeinitScript);
        }
        return result;
    }

    private void AddSystemSamples()
    {
        Samples.Add(new MessageBoxMessage()
        {
            Path = $"{Configuration.Name}/$SYSTEM/ExecutionDuration",
            Data = Runner.ExecutionDuration,
            Timestamp = DateTime.UtcNow.ToEpochMilliseconds(),
            ConnectorItemRef = new ConnectorItem()
            {
                Configuration = Configuration,
                ReportByException = false
            }
        });
        Logger.Debug($"[{Configuration.Name}] Add Sample. Emit $SYSTEM/ExecutionDuration = {Runner.ExecutionDuration}");
        
        Samples.Add(new MessageBoxMessage()
        {
            Path = $"{Configuration.Name}/$SYSTEM/IsConnected",
            Data = IsConnected,
            Timestamp = DateTime.UtcNow.ToEpochMilliseconds(),
            ConnectorItemRef = new ConnectorItem()
            {
                Configuration = Configuration,
                ReportByException = true
            }
        });
        Logger.Debug($"[{Configuration.Name}] Add Sample. Emit $SYSTEM/IsConnected = {IsConnected}");
   
        Samples.Add(new MessageBoxMessage()
        {
            Path = $"{Configuration.Name}/$SYSTEM/IsFaulted",
            Data = IsFaulted,
            Timestamp = DateTime.UtcNow.ToEpochMilliseconds(),
            ConnectorItemRef = new ConnectorItem()
            {
                Configuration = Configuration,
                ReportByException = true
            }
        });
        Logger.Debug($"[{Configuration.Name}] Add Sample. Emit $SYSTEM/IsFaulted = {IsFaulted}");
        
        Samples.Add(new MessageBoxMessage()
        {
            Path = $"{Configuration.Name}/$SYSTEM/Fault",
            Data = FaultReason is null ? null : FaultReason.Message,
            Timestamp = DateTime.UtcNow.ToEpochMilliseconds(),
            ConnectorItemRef = new ConnectorItem()
            {
                Configuration = Configuration,
                ReportByException = true
            }
        });
        Logger.Debug($"[{Configuration.Name}] Add Sample. Emit $SYSTEM/Fault = {(FaultReason is null ? "<null>" : FaultReason)}");
    }
    
    private void FillInbox()
    {
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
                Logger.Trace($"[{sampleResponse.Path}] Fill Inbox. New sample added to inbox. " +
                             $"Sample={(sampleResponse.Data is null ? "<null>" : JsonConvert.SerializeObject(sampleResponse.Data))}");
                
                Inbox.Add(sampleResponse);
                Current.Add(sampleResponse);
            }
            // sample data is different, it is an updated sample
            else
            {
                var confRbe = Configuration.ReportByException;
                var itemRbe = sampleResponse.ConnectorItemRef is null || sampleResponse.ConnectorItemRef.ReportByException;

                // rbe is enabled
                if (!confRbe && itemRbe || confRbe && itemRbe)
                {
                    // either data is null, compare datas as object
                    if (matchingCurrent.Data is null || sampleResponse.Data is null)
                    {
                        if (matchingCurrent.Data != sampleResponse.Data)
                        {
                            Logger.Trace($"[{sampleResponse.Path}] Fill Inbox. RBE (null check) sample object added to inbox. " +
                                         $"Current={(matchingCurrent.Data is null ? "<null>": matchingCurrent.Data)}, " +
                                         $"Sample={(sampleResponse.Data is null ? "<null>": sampleResponse.Data)}");

                            Inbox.Add(sampleResponse);
                        }
                        else
                        {
                            Logger.Trace($"[{sampleResponse.Path}] Fill Inbox. RBE (null check) sample object dropped. " +
                                         $"Current={(matchingCurrent.Data is null ? "<null>": matchingCurrent.Data)}, " +
                                         $"Sample={(sampleResponse.Data is null ? "<null>": sampleResponse.Data)}");
                        }
                    }
                    // datas are not null
                    else
                    {
                        bool isCurrentArray = matchingCurrent.Data.GetType().IsArray;
                        bool isSampleArray = sampleResponse.Data.GetType().IsArray;

                        // both datas are array, compare datas as sequence
                        if (isCurrentArray && isSampleArray)
                        {
                            if (!((object[])matchingCurrent.Data).SequenceEqual((object[])sampleResponse.Data))
                            {
                                Logger.Trace($"[{sampleResponse.Path}] Fill Inbox. RBE sample array added to inbox. " +
                                             $"Current={(matchingCurrent.Data is null ? "<null>" : JsonConvert.SerializeObject(matchingCurrent.Data))}, " +
                                             $"Sample={(sampleResponse.Data is null ? "<null>" : JsonConvert.SerializeObject(sampleResponse.Data))}");

                                Inbox.Add(sampleResponse);
                            }
                            else
                            {
                                Logger.Trace($"[{sampleResponse.Path}] Fill Inbox. RBE sample array dropped. " +
                                             $"Current={(matchingCurrent.Data is null ? "<null>" : JsonConvert.SerializeObject(matchingCurrent.Data))}, " +
                                             $"Sample={(sampleResponse.Data is null ? "<null>" : JsonConvert.SerializeObject(sampleResponse.Data))}");
                            }
                        }
                        // both datas are not arrays, compare datas as object
                        else
                        {
                            if (!matchingCurrent.Data.Equals(sampleResponse.Data))
                            {
                                Logger.Trace($"[{sampleResponse.Path}] Fill Inbox. RBE sample object added to inbox. " +
                                             $"Current={(matchingCurrent.Data is null ? "<null>" : JsonConvert.SerializeObject(matchingCurrent.Data))}, " +
                                             $"Sample={(sampleResponse.Data is null ? "<null>" : JsonConvert.SerializeObject(sampleResponse.Data))}");

                                Inbox.Add(sampleResponse);
                            }
                            else
                            {
                                Logger.Trace($"[{sampleResponse.Path}] Fill Inbox. RBE sample object dropped. " +
                                             $"Current={(matchingCurrent.Data is null ? "<null>" : JsonConvert.SerializeObject(matchingCurrent.Data))}, " +
                                             $"Sample={(sampleResponse.Data is null ? "<null>" : JsonConvert.SerializeObject(sampleResponse.Data))}");
                            }
                        }
                    }
                }
                // rbe is disabled
                else
                {
                    Logger.Trace($"[{sampleResponse.Path}] Fill Inbox. Non-RBE sample added to inbox. " +
                                 $"Sample={(sampleResponse.Data is null ? "<null>" : JsonConvert.SerializeObject(sampleResponse.Data))}");
                    
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