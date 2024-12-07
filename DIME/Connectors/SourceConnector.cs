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
    public ConcurrentDictionary<string, MessageBoxMessage> Current { get; set; }
    protected bool PublishInboxInBatch { get; set; }
    
    public SourceConnector(TConfig configuration, Disruptor.Dsl.Disruptor<MessageBoxMessage> disruptor): base(configuration, disruptor)
    {
        ScriptRunner = new LuaRunner();
        Inbox = new ConcurrentBag<MessageBoxMessage>();
        Samples = new ConcurrentBag<MessageBoxMessage>();
        Current = new ConcurrentDictionary<string, MessageBoxMessage>();
        PublishInboxInBatch = true;
        
        Logger.Trace($"[{Configuration.Name}] SourceConnector:.ctor");
    }

    public override bool Initialize(ConnectorRunner runner)
    {
        Logger.Trace($"[{Configuration.Name}] SourceConnector:Initialize::ENTER");
        
        var result = base.Initialize(runner) && ScriptRunner.Initialize(this);
        if (!string.IsNullOrEmpty(Configuration.InitScript))
        {
            ExecuteScript(Configuration.InitScript);
        }
        
        Logger.Trace($"[{Configuration.Name}] SourceConnector:Initialize::EXIT");
        
        return result;
    }

    protected object ExecuteScript(string script)
    {
        Logger.Trace($"[{Configuration.Name}] SourceConnector:ExecuteScript::ENTER");

        object response = null;

        try
        {
            var scriptResult = ScriptRunner.DoString(script);
            response = scriptResult.Length == 1 ? scriptResult[0] : scriptResult;
        }
        catch (Exception e)
        {
            response = null;
        }
        
        Logger.Trace($"[{Configuration.Name}] SourceConnector:ExecuteScript::EXIT");
        
        return response;
    }
    
    protected object ExecuteScript(object intermediateResult, ConnectorItem item)
    {
        Logger.Trace($"[{Configuration.Name}] SourceConnector:ExecuteScript::ENTER");

        object response = null;
        
        try
        {
            ScriptRunner["result"] = intermediateResult;
            var scriptResult = ScriptRunner.DoString(item);
            response =  scriptResult.Length == 1 ? scriptResult[0] : scriptResult;
        }
        catch (Exception e)
        {
            response = null;
        }
        
        Logger.Trace($"[{Configuration.Name}] SourceConnector:ExecuteScript::EXIT");
        
        return response;
    }
    
    public override bool BeforeUpdate()
    {
        Logger.Trace($"[{Configuration.Name}] SourceConnector:BeforeUpdate::ENTER");
        
        Samples.Clear();
        
        Logger.Trace($"[{Configuration.Name}] SourceConnector:BeforeUpdate::EXIT");
        
        return true;
    }
    
    protected abstract bool ReadImplementation();

    public virtual bool Read()
    {
        Logger.Trace($"[{Configuration.Name}] SourceConnector:Read::ENTER");
        
        FaultContext = FaultContextEnum.Read;

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
                result = ReadImplementation();

                if (result)
                {
                    ClearFault();
                }
                else
                {
                    MarkFaulted(new Exception("Device implementation read failed."));
                }
            }
            catch (Exception e)
            {
                MarkFaulted(e);
                Disconnect();
                result = false;
            }
        }
        
        Logger.Trace($"[{Configuration.Name}] SourceConnector:Read::EXIT");
        
        return result;
    }

    public override bool AfterUpdate()
    {
        Logger.Trace($"[{Configuration.Name}] SourceConnector:AfterUpdate::ENTER");
        
        AddSystemSamples();
        FillInbox();
        PublishInbox();
        Inbox.Clear();
        
        Logger.Trace($"[{Configuration.Name}] SourceConnector:AfterUpdate::EXIT");
        
        return true;
    }
    
    public override bool Deinitialize()
    {
        Logger.Trace($"[{Configuration.Name}] SourceConnector:Deinitialize::ENTER");
        
        var result = base.Deinitialize();
        if (!string.IsNullOrEmpty(Configuration.DeinitScript))
        {
            ExecuteScript(Configuration.DeinitScript);
        }
        
        Logger.Trace($"[{Configuration.Name}] SourceConnector:Deinitialize::EXIT");
        
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
            Current.TryGetValue(sampleResponse.Path, out matchingCurrent);
            
            // sample does not exist in current, it is a new sample
            if (matchingCurrent is null)
            {
                Logger.Debug($"[{sampleResponse.Path}] Fill Inbox. New sample added to inbox. " +
                             $"Sample={(sampleResponse.Data is null ? "<null>" : JsonConvert.SerializeObject(sampleResponse.Data))}");
                
                Inbox.Add(sampleResponse);
                Current.AddOrUpdate(sampleResponse.Path, sampleResponse, (key, oldValue) => sampleResponse);
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
                            Logger.Debug($"[{sampleResponse.Path}] Fill Inbox. RBE (null check) sample object added to inbox. " +
                                         $"Current={(matchingCurrent.Data is null ? "<null>": matchingCurrent.Data)}, " +
                                         $"Sample={(sampleResponse.Data is null ? "<null>": sampleResponse.Data)}");

                            Inbox.Add(sampleResponse);
                        }
                        else
                        {
                            Logger.Debug($"[{sampleResponse.Path}] Fill Inbox. RBE (null check) sample object dropped. " +
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
                                Logger.Debug($"[{sampleResponse.Path}] Fill Inbox. RBE sample array added to inbox. " +
                                             $"Current={(matchingCurrent.Data is null ? "<null>" : JsonConvert.SerializeObject(matchingCurrent.Data))}, " +
                                             $"Sample={(sampleResponse.Data is null ? "<null>" : JsonConvert.SerializeObject(sampleResponse.Data))}");

                                Inbox.Add(sampleResponse);
                            }
                            else
                            {
                                Logger.Debug($"[{sampleResponse.Path}] Fill Inbox. RBE sample array dropped. " +
                                             $"Current={(matchingCurrent.Data is null ? "<null>" : JsonConvert.SerializeObject(matchingCurrent.Data))}, " +
                                             $"Sample={(sampleResponse.Data is null ? "<null>" : JsonConvert.SerializeObject(sampleResponse.Data))}");
                            }
                        }
                        // both datas are not arrays, compare datas as object
                        else
                        {
                            if (!matchingCurrent.Data.Equals(sampleResponse.Data))
                            {
                                Logger.Debug($"[{sampleResponse.Path}] Fill Inbox. RBE sample object added to inbox. " +
                                             $"Current={(matchingCurrent.Data is null ? "<null>" : JsonConvert.SerializeObject(matchingCurrent.Data))}, " +
                                             $"Sample={(sampleResponse.Data is null ? "<null>" : JsonConvert.SerializeObject(sampleResponse.Data))}");

                                Inbox.Add(sampleResponse);
                            }
                            else
                            {
                                Logger.Debug($"[{sampleResponse.Path}] Fill Inbox. RBE sample object dropped. " +
                                             $"Current={(matchingCurrent.Data is null ? "<null>" : JsonConvert.SerializeObject(matchingCurrent.Data))}, " +
                                             $"Sample={(sampleResponse.Data is null ? "<null>" : JsonConvert.SerializeObject(sampleResponse.Data))}");
                            }
                        }
                    }
                }
                // rbe is disabled
                else
                {
                    Logger.Debug($"[{sampleResponse.Path}] Fill Inbox. Non-RBE sample added to inbox. " +
                                 $"Sample={(sampleResponse.Data is null ? "<null>" : JsonConvert.SerializeObject(sampleResponse.Data))}");
                    
                    Inbox.Add(sampleResponse);
                }

                Current.AddOrUpdate(sampleResponse.Path, sampleResponse, (key, oldValue) => sampleResponse);
            }
        }
    }

    private void PublishInbox()
    {
        if (Inbox.Count > 0)
        {
            if (PublishInboxInBatch)
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
            else
            {
                foreach (var response in Inbox)
                {
                    using (var scope = Disruptor.RingBuffer.PublishEvent())
                    {
                        var data = scope.Event();
                        data.Data = response.Data;
                        data.Path = response.Path;
                        data.Timestamp = response.Timestamp;
                        data.ConnectorItemRef = response.ConnectorItemRef;
                    }
                }
            }
        }
    }
}