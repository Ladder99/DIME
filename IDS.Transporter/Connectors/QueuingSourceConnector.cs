using System.Collections.Concurrent;
using IDS.Transporter.Configuration;
using Newtonsoft.Json;

namespace IDS.Transporter.Connectors;

public abstract class QueuingSourceConnector<TConfig, TItem>: SourceConnector<TConfig, TItem>
    where TConfig : ConnectorConfiguration<TItem>
    where TItem : ConnectorItem
{
    protected class IncomingMessage
    {
        public string Key { get; set; }
        public string Value { get; set; }
        public long Timestamp { get; set; }
    }
    
    // message hold
    protected readonly ConcurrentBag<IncomingMessage> _incomingBuffer = new();
    protected readonly object _incomingBufferLock = new();
    
    public QueuingSourceConnector(TConfig configuration, Disruptor.Dsl.Disruptor<MessageBoxMessage> disruptor) : base(configuration, disruptor)
    {
    }

    protected override bool ReadImplementation()
    {
        if (Configuration.ItemizedRead)
        {
            /*
             * itemized read iterates through connector items
             * find incoming buffer messages that matches connector item or execute connector item script
             */
            
            lock (_incomingBufferLock)
            {
                foreach (var item in Configuration.Items.Where(x => x.Enabled))
                {
                    IEnumerable<IncomingMessage> messages = null;

                    if (item.Address != null)
                    {
                        messages = _incomingBuffer.Where(x => x.Key == item.Address);

                        foreach (var message in messages)
                        {
                            object result = message.Value;
                            object readResult = result;
                            object scriptResult = "n/a";

                            if (item.Script != null)
                            {
                                result = ExecuteScript(message.Value, item);
                                scriptResult = result;
                            }

                            if (result != null)
                            {
                                Samples.Add(new MessageBoxMessage()
                                {
                                    Path = $"{Configuration.Name}/{item.Name}",
                                    Data = result,
                                    Timestamp = DateTime.UtcNow.ToEpochMilliseconds(),
                                    ConnectorItemRef = item
                                });
                            }
                            
                            Logger.Trace($"[{Configuration.Name}/{item.Name}] Read Impl. " +
                                         $"Read={(readResult==null ? "<null>" : JsonConvert.SerializeObject(readResult))}, " +
                                         $"Script={(scriptResult==null ? "<null>" : JsonConvert.SerializeObject(scriptResult))}, " +
                                         $"Sample={(result == null ? "DROPPED" : "ADDED")}");
                        }
                    }
                    else if (item.Script != null)
                    {
                        var result = ExecuteScript(null, item);

                        if (result != null)
                        {
                            Samples.Add(new MessageBoxMessage()
                            {
                                Path = $"{Configuration.Name}/{item.Name}",
                                Data = result,
                                Timestamp = DateTime.UtcNow.ToEpochMilliseconds(),
                                ConnectorItemRef = item
                            });
                        }
                        
                        Logger.Trace($"[{Configuration.Name}/{item.Name}] Read Impl. " +
                                     $"Read=<null>, " +
                                     $"Script={(result==null ? "<null>" : JsonConvert.SerializeObject(result))}, " +
                                     $"Sample={(result == null ? "DROPPED" : "ADDED")}");
                    }
                }

                _incomingBuffer.Clear();
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
            lock (_incomingBufferLock)
            {
                foreach (var message in _incomingBuffer.ToArray())
                {
                    try
                    {
                        var item = Configuration.Items
                            .First(x => x.Enabled && x.Address == message.Key && x.Script != null);
                        
                        var result = ExecuteScript(message.Value, item);

                        if (result != null)
                        {
                            Samples.Add(new MessageBoxMessage()
                            {
                                Path = $"{Configuration.Name}/{message.Key}",
                                Data = result,
                                Timestamp = message.Timestamp,
                                ConnectorItemRef = item
                            });
                        }
                    }
                    catch (InvalidOperationException e)
                    {
                        Samples.Add(new MessageBoxMessage()
                        {
                            Path = $"{Configuration.Name}/{message.Key}",
                            Data = message.Value,
                            Timestamp = message.Timestamp,
                            ConnectorItemRef = new ConnectorItem()
                            {
                                Configuration = Configuration
                            }
                        });
                    }
                }

                _incomingBuffer.Clear();
            }
        }

        return true;
    }
}