using DIME.Configuration;
using Newtonsoft.Json;

namespace DIME.Connectors;

public abstract class PollingSourceConnector<TConfig, TItem>: SourceConnector<TConfig, TItem>
    where TConfig : ConnectorConfiguration<TItem>
    where TItem : ConnectorItem
{
    public PollingSourceConnector(TConfig configuration, Disruptor.Dsl.Disruptor<MessageBoxMessage> disruptor) : base(configuration, disruptor)
    {
    }

    protected abstract object ReadFromDevice(TItem item);
    
    protected override bool ReadImplementation()
    {
        /*
         * iterate through connector items
         *   execute read from device
         *   execute script
         */
        foreach (var item in Configuration.Items.Where(x => x.Enabled))
        {
            object response = null;
            object readResult = "n/a";
            object scriptResult = "n/a";
            
            if (item.Address != null)
            {
                response = ReadFromDevice(item);
                readResult = response;
            }

            if (item.Script != null)
            {
                response = ExecuteScript(response, item);
                scriptResult = response;
            }
            
            if (response != null)
            {
                Samples.Add(new MessageBoxMessage()
                {
                    Path = $"{Configuration.Name}/{item.Name}",
                    Data = response,
                    Timestamp = DateTime.UtcNow.ToEpochMilliseconds(),
                    ConnectorItemRef = item
                });
            }
            
            Logger.Trace($"[{Configuration.Name}/{item.Name}] Read Impl. " +
                         $"Read={(readResult==null ? "<null>" : JsonConvert.SerializeObject(readResult))}, " +
                         $"Script={(scriptResult==null ? "<null>" : JsonConvert.SerializeObject(scriptResult))}, " +
                         $"Sample={(response == null ? "DROPPED" : "ADDED")}");
        }
        
        return true;
    }

}