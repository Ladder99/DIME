using System.Diagnostics;
using DIME.Configuration;
using Newtonsoft.Json;

namespace DIME.Connectors;

public abstract class PollingSourceConnector<TConfig, TItem>: SourceConnector<TConfig, TItem>
    where TConfig : ConnectorConfiguration<TItem>
    where TItem : ConnectorItem
{
    private Stopwatch _readFromDeviceSumStopwatch = new Stopwatch();
    private Stopwatch _executeScriptSumStopwatch = new Stopwatch();
    private Stopwatch _entireReadLoopStopwatch = new Stopwatch();
    
    public PollingSourceConnector(TConfig configuration, Disruptor.Dsl.Disruptor<MessageBoxMessage> disruptor) : base(configuration, disruptor)
    {
        Logger.Trace($"[{Configuration.Name}] PollingSourceConnector:.ctor");
    }

    protected abstract object ReadFromDevice(TItem item);
    
    protected override bool ReadImplementation()
    {
        Logger.Trace($"[{Configuration.Name}] PollingSourceConnector:ReadImplementation::ENTER");
        _entireReadLoopStopwatch.Start();
        
        /*
         * iterate through connector items
         *   execute read from device
         *   execute script
         */
        if (!string.IsNullOrEmpty(Configuration.LoopEnterScript))
        {
            ExecuteScript(Configuration.LoopEnterScript);
        }
        
        foreach (var item in Configuration.Items.Where(x => x.Enabled))
        {
            object response = null;
            object readResult = "n/a";
            object scriptResult = "n/a";
            
            _readFromDeviceSumStopwatch.Start();
            if (!string.IsNullOrEmpty(item.Address))
            {
                response = ReadFromDevice(item);
                readResult = response;
            }
            _readFromDeviceSumStopwatch.Stop();

            //Console.WriteLine($"SCRIPT: {item.Script}");
            _executeScriptSumStopwatch.Start();
            if (!string.IsNullOrEmpty(item.Script))
            {
                response = ExecuteScript(response, item);
                scriptResult = response;
            }
            _executeScriptSumStopwatch.Stop();

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
        
        if (!string.IsNullOrEmpty(Configuration.LoopExitScript))
        {
            ExecuteScript(Configuration.LoopExitScript);
        }
        
        Logger.Trace($"[{Configuration.Name}] PollingSourceConnector:ReadImplementation::EXIT");
        
        Logger.Info($"[{Configuration.Name}] Read Loop Perf. " +
                    $"DeviceRead: {_readFromDeviceSumStopwatch.ElapsedMilliseconds}ms, " +
                    $"ExecuteScript: {_executeScriptSumStopwatch.ElapsedMilliseconds}ms, " +
                    $"EntireLoop: {_entireReadLoopStopwatch.ElapsedMilliseconds}ms");
        _readFromDeviceSumStopwatch.Reset();
        _executeScriptSumStopwatch.Reset();
        _entireReadLoopStopwatch.Reset();
        return true;
    }

}