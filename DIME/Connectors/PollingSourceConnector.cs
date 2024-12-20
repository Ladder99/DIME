using System.Diagnostics;
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
            ExecuteScript(Configuration.LoopEnterScript, this);
        }
        
        foreach (var item in Configuration.Items.Where(x => x.Enabled))
        {
            object response = null;
            object readResult = "n/a";
            object scriptResult = "n/a";
            
            ReadFromDeviceSumStopwatch.Start();
            if (!string.IsNullOrEmpty(item.Address))
            {
                response = ReadFromDevice(item);
                readResult = response;
            }
            
            TagValues[$"{Configuration.Name}/{item.Name}"] = new MessageBoxMessage()
            {
                Path = Configuration.StripPathPrefix ? item.Name : $"{Configuration.Name}/{item.Name}",
                Data = readResult,
                Timestamp = DateTime.Now.ToEpochMilliseconds(),
                ConnectorItemRef = item
            };
            
            ReadFromDeviceSumStopwatch.Stop();

            //Console.WriteLine($"SCRIPT: {item.Script}");
            ExecuteScriptSumStopwatch.Start();
            if (!string.IsNullOrEmpty(item.Script))
            {
                response = ExecuteScript(response, item);
                scriptResult = response;
            }
            ExecuteScriptSumStopwatch.Stop();

            /*
            try
            {
                Console.WriteLine($"RESULT: {scriptResult}");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
            */
            
            if (response is not null)
            {
                Samples.Add(new MessageBoxMessage()
                {
                    Path = Configuration.StripPathPrefix ? item.Name : $"{Configuration.Name}/{item.Name}",
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