using System.Data;
using DIME.Configuration;
using Newtonsoft.Json;

namespace DIME.Connectors;

public abstract class DatabaseSourceConnector<TConfig, TItem>: SourceConnector<TConfig, TItem>
    where TConfig : DatabaseConnectorConfiguration<TItem>
    where TItem : ConnectorItem
{
    public DatabaseSourceConnector(TConfig configuration, Disruptor.Dsl.Disruptor<MessageBoxMessage> disruptor) : base(configuration, disruptor)
    {
        Logger.Trace($"[{Configuration.Name}] PollingSourceConnector:.ctor");
    }

    protected abstract DataTable ReadFromDevice();
    
    protected override bool ReadImplementation()
    {
        Logger.Trace($"[{Configuration.Name}] PollingSourceConnector:ReadImplementation::ENTER");
        EntireReadLoopStopwatch.Start();
        
        if (!string.IsNullOrEmpty(Configuration.LoopEnterScript))
        {
            ExecuteScript(Configuration.LoopEnterScript, this);
        }
        
        ReadFromDeviceSumStopwatch.Start();
        var dataTable = ReadFromDevice();
        ReadFromDeviceSumStopwatch.Stop();
        
        foreach (var item in Configuration.Items.Where(x => x.Enabled))
        {
            object response = null;
            object readResult = null;
            object scriptResult = null;
            
            if (!string.IsNullOrEmpty(item.Address))
            {
                response = dataTable.AsEnumerable()
                    .Select(row => row.Field<object>(item.Address))
                    .ToArray();
                readResult = response;
            }
            
            AddMessageToTagValues(item, readResult);
            
            ExecuteScriptSumStopwatch.Start();
            if (ItemOrConfigurationHasItemScript(item))
            {
                response = ExecuteScript(response, item);
                scriptResult = response;
            }
            ExecuteScriptSumStopwatch.Stop();

            if (response is not null)
            {
                AddMessageToTagValues(item, response);
            }
            
            Logger.Trace($"[{Configuration.Name}/{item.Name}] Read Impl. " +
                         $"Read={(readResult==null ? "<null>" : JsonConvert.SerializeObject(readResult))}, " +
                         $"Script={(scriptResult==null ? "<null>" : JsonConvert.SerializeObject(scriptResult))}, " +
                         $"Sample={(response == null ? "DROPPED" : "ADDED")}");
        }
        
        if (!string.IsNullOrEmpty(Configuration.LoopExitScript))
        {
            ExecuteScript(Configuration.LoopExitScript, this);
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