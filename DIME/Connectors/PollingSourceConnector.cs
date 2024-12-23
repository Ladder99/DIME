using DIME.Configuration;
using Newtonsoft.Json;

namespace DIME.Connectors;

public abstract class PollingSourceConnector<TConfig, TItem>: SourceConnector<TConfig, TItem>
    where TConfig : ConnectorConfiguration<TItem>
    where TItem : ConnectorItem
{
    public PollingSourceConnector(TConfig configuration, Disruptor.Dsl.Disruptor<MessageBoxMessage> disruptor) : base(configuration, disruptor)
    {
        Logger.Trace($"[{Configuration.Name}] PollingSourceConnector:.ctor");
    }

    protected abstract object ReadFromDevice(TItem item);
    
    protected override bool ReadImplementation()
    {
        Logger.Trace($"[{Configuration.Name}] PollingSourceConnector:ReadImplementation::ENTER");
        EntireReadLoopStopwatch.Start();
        
        /*
         * iterate through connector items
         *   execute read from device
         *   execute script
         */
        if (!string.IsNullOrEmpty(Configuration.LoopEnterScript))
        {
            ExecuteScriptSumStopwatch.Start();
            ExecuteScript(Configuration.LoopEnterScript, this);
            ExecuteScriptSumStopwatch.Stop();
        }
        
        foreach (var item in Configuration.Items.Where(x => x.Enabled))
        {
            object response = null;
            object readResult = null;
            object scriptResult = null;
            
            if (!string.IsNullOrEmpty(item.Address))
            {
                ReadFromDeviceSumStopwatch.Start();
                response = ReadFromDevice(item);
                readResult = response;
                ReadFromDeviceSumStopwatch.Stop();
            }
            
            AddMessageToTagValues(item, readResult);
            
            if (ItemOrConfigurationHasItemScript(item))
            {
                ExecuteScriptSumStopwatch.Start();
                response = ExecuteScript(response, item);
                scriptResult = response;
                ExecuteScriptSumStopwatch.Stop();
            }
            
            if (response is not null)
            {
                AddMessageToSamples(item, response);
            }
            
            Logger.Trace($"[{Configuration.Name}/{item.Name}] Read Impl. " +
                         $"Read={(readResult==null ? "<null>" : JsonConvert.SerializeObject(readResult))}, " +
                         $"Script={(scriptResult==null ? "<null>" : JsonConvert.SerializeObject(scriptResult))}, " +
                         $"Sample={(response == null ? "DROPPED" : "ADDED")}");
        }
        
        if (!string.IsNullOrEmpty(Configuration.LoopExitScript))
        {
            ExecuteScriptSumStopwatch.Start();
            ExecuteScript(Configuration.LoopExitScript, this);
            ExecuteScriptSumStopwatch.Stop();
        }
        
        EntireReadLoopStopwatch.Stop();
        Logger.Trace($"[{Configuration.Name}] PollingSourceConnector:ReadImplementation::EXIT");
        
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