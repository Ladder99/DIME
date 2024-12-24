using DIME.Configuration;
using Newtonsoft.Json;

namespace DIME.Connectors;

public abstract class BatchPollingSourceConnector<TConfig, TItem>: SourceConnector<TConfig, TItem>
    where TConfig : ConnectorConfiguration<TItem>
    where TItem : ConnectorItem
{
    public BatchPollingSourceConnector(TConfig configuration, Disruptor.Dsl.Disruptor<MessageBoxMessage> disruptor) : base(configuration, disruptor)
    {
        Logger.Trace($"[{Configuration.Name}] BatchPollingSourceConnector:.ctor");
    }

    protected abstract bool ReadBatchFromDevice();

    protected abstract object ReadItemFromBatch(ConnectorItem item);
    
    protected override bool ReadImplementation()
    {
        Logger.Trace($"[{Configuration.Name}] BatchPollingSourceConnector:ReadImplementation::ENTER");
        EntireReadLoopStopwatch.Start();
        
        if (!string.IsNullOrEmpty(Configuration.LoopEnterScript))
        {
            ExecuteScriptSumStopwatch.Start();
            ExecuteScript(Configuration.LoopEnterScript, this);
            ExecuteScriptSumStopwatch.Stop();
        }
        
        ReadFromDeviceSumStopwatch.Start();
        ReadBatchFromDevice();
        ReadFromDeviceSumStopwatch.Stop();
        
        foreach (var item in Configuration.Items.Where(x => x.Enabled))
        {
            object response = null;
            object readResult = null;
            object scriptResult = null;
            
            if (!string.IsNullOrEmpty(item.Address))
            {
                ReadFromDeviceSumStopwatch.Start();
                response = ReadItemFromBatch(item);
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
        Logger.Trace($"[{Configuration.Name}] BatchPollingSourceConnector:ReadImplementation::EXIT");
        
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