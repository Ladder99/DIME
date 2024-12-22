using System.Collections.Concurrent;
using DIME.Configuration;
using Newtonsoft.Json;

namespace DIME.Connectors;

public abstract class QueuingSourceConnector<TConfig, TItem>: SourceConnector<TConfig, TItem>
    where TConfig : ConnectorConfiguration<TItem>
    where TItem : ConnectorItem
{
    protected class IncomingMessage
    {
        public string Key { get; set; }
        public object Value { get; set; }
        public long Timestamp { get; set; }
    }
    
    // message hold
    protected readonly ConcurrentBag<IncomingMessage> IncomingBuffer = new();
    protected readonly object IncomingBufferLock = new();
    
    public QueuingSourceConnector(TConfig configuration, Disruptor.Dsl.Disruptor<MessageBoxMessage> disruptor) : base(configuration, disruptor)
    {
        Logger.Trace($"[{Configuration.Name}] QueuingSourceConnector:.ctor");
    }

    protected IncomingMessage AddToIncomingBuffer(string key, object value, long timestamp = 0)
    {
        ReadFromDeviceSumStopwatch.Start();
        lock (IncomingBufferLock)
        {
            var message = new IncomingMessage()
            {
                Key = key,
                Value = value,
                Timestamp = timestamp == 0 ? DateTime.Now.ToEpochMilliseconds() : timestamp
            };
            
            IncomingBuffer.Add(message);
            ReadFromDeviceSumStopwatch.Stop();
        
            return message;
        }
    }
    
    protected override bool ReadImplementation()
    {
        Logger.Trace($"[{Configuration.Name}] QueuingSourceConnector:ReadImplementation::ENTER");
        EntireReadLoopStopwatch.Start();
        
        if (!string.IsNullOrEmpty(Configuration.LoopEnterScript))
        {
            ExecuteScript(Configuration.LoopEnterScript, this);
        }
        
        if (Configuration.ItemizedRead)
        {
            /*
             * itemized read iterates through connector items
             * find incoming buffer messages that matches connector item or execute connector item script
             */
            
            lock (IncomingBufferLock)
            {
                foreach (var item in Configuration.Items.Where(x => x.Enabled))
                {
                    IEnumerable<IncomingMessage> messages = null;

                    if (!string.IsNullOrEmpty(item.Address))
                    {
                        messages = IncomingBuffer.Where(x => x.Key == item.Address);

                        foreach (var message in messages)
                        {
                            object result = message.Value;
                            object readResult = result;
                            object scriptResult = null;

                            AddMessageToTagValues(item, readResult);
                            
                            ExecuteScriptSumStopwatch.Start();
                            if (ItemOrConfigurationHasItemScript(item))
                            {
                                result = ExecuteScript(message.Value, item);
                                scriptResult = result;
                            }
                            ExecuteScriptSumStopwatch.Stop();

                            if (result is not null)
                            {
                                AddMessageToSamples(item, result);
                            }
                            
                            Logger.Trace($"[{Configuration.Name}/{item.Name}] Read Impl. " +
                                         $"Read={(readResult==null ? "<null>" : JsonConvert.SerializeObject(readResult))}, " +
                                         $"Script={(scriptResult==null ? "<null>" : JsonConvert.SerializeObject(scriptResult))}, " +
                                         $"Sample={(result == null ? "DROPPED" : "ADDED")}");
                        }
                    }
                    else if (ItemOrConfigurationHasItemScript(item))
                    {
                        ExecuteScriptSumStopwatch.Start();
                        var result = ExecuteScript(null, item);
                        ExecuteScriptSumStopwatch.Stop();
                        
                        if (result is not null)
                        {
                            AddMessageToSamples(item, result);
                        }
                        
                        Logger.Trace($"[{Configuration.Name}/{item.Name}] Read Impl. " +
                                     $"Read=<null>, " +
                                     $"Script={(result==null ? "<null>" : JsonConvert.SerializeObject(result))}, " +
                                     $"Sample={(result == null ? "DROPPED" : "ADDED")}");
                    }
                }

                IncomingBuffer.Clear();
            }
        }
        else
        {
            /*
             * non-itemized read iterates through the incoming buffer
             * find connector item for corresponding incoming buffer message
             * evaluate script against connector item or
             * use the incoming buffer message value
             */
            lock (IncomingBufferLock)
            {
                //TODO: why ToArray?
                foreach (var message in IncomingBuffer.ToArray())
                {
                    try
                    {
                        var item = Configuration.Items
                            .First(x => x.Enabled && x.Address == message.Key);
                        
                        AddMessageToTagValues(item, message.Value);

                        var result = message.Value;
                        object readResult = result;
                        object scriptResult = null;

                        if (ItemOrConfigurationHasItemScript(item))
                        {
                            result = ExecuteScript(message.Value, item);
                            scriptResult = result;
                        }

                        if (result is not null)
                        {
                            AddMessageToSamples(item, result);
                        }
                        
                        Logger.Trace($"[{Configuration.Name}/{item.Name}] Read Impl. " +
                                     $"Read={(readResult==null ? "<null>" : JsonConvert.SerializeObject(readResult))}, " +
                                     $"Script={(scriptResult==null ? "<null>" : JsonConvert.SerializeObject(scriptResult))}, " +
                                     $"Sample={(result == null ? "DROPPED" : "ADDED")}");
                    }
                    catch (InvalidOperationException e)
                    {
                        AddMessageToSamples(message.Key, message.Value, message.Timestamp);
                    }
                }

                IncomingBuffer.Clear();
            }
        }

        if (!string.IsNullOrEmpty(Configuration.LoopExitScript))
        {
            ExecuteScript(Configuration.LoopExitScript, this);
        }
        
        EntireReadLoopStopwatch.Stop();
        Logger.Trace($"[{Configuration.Name}] QueuingSourceConnector:ReadImplementation::ENTER");
        
        Logger.Debug($"[{Configuration.Name}] Loop Perf. " +
                    $"DeviceRead: {ReadFromDeviceSumStopwatch.ElapsedMilliseconds}ms, " +
                    $"ExecuteScript: {ExecuteScriptSumStopwatch.ElapsedMilliseconds}ms, " +
                    $"EntireLoop: {EntireReadLoopStopwatch.ElapsedMilliseconds}ms");
        
        base.InvokeOnLoopPerf(
            ReadFromDeviceSumStopwatch.ElapsedMilliseconds,
            ExecuteScriptSumStopwatch.ElapsedMilliseconds,
            EntireReadLoopStopwatch.ElapsedMilliseconds);
        
        ReadFromDeviceSumStopwatch.Reset();
        ExecuteScriptSumStopwatch.Reset();
        EntireReadLoopStopwatch.Reset();
        return true;
    }
}