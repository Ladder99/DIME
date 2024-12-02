using IDS.Transporter.Configuration;
using IDS.Transporter.Connectors;
using Timer = System.Timers.Timer;

namespace IDS.Transporter;

public class ConnectorRunner
{
    protected readonly NLog.Logger Logger;
    public List<ConnectorRunner> Runners { get;}
    public  IConnector Connector { get; }
    private Disruptor.Dsl.Disruptor<MessageBoxMessage> _disruptor;
    private Timer _timer;
    private bool _isExecuting;
    private long _executionEnter = DateTime.UtcNow.ToEpochMilliseconds();
    private long _executionExit = DateTime.UtcNow.ToEpochMilliseconds();
    public long ExecutionDuration { get; private set; }

    public ConnectorRunner(List<ConnectorRunner> runners, IConnector connector, Disruptor.Dsl.Disruptor<MessageBoxMessage> disruptor)
    {
        Logger = NLog.LogManager.GetLogger(GetType().FullName);
        Runners = runners;
        Connector = connector;
        _disruptor = disruptor;
    }
    
    public void Start()
    {
        if (Connector.Configuration.Direction == ConnectorDirectionEnum.Sink)
        {
            _disruptor.HandleEventsWith(new SinkMessageHandler((ISinkConnector)Connector));
        }

        ConnectorInitialize();
        ConnectorCreate();
        StartTimer();
    }

    private void Execute()
    {
        if (!ExecuteEnter())
        {
            return;
        }

        ConnectorConnect();

        if (Connector.Configuration.Direction == ConnectorDirectionEnum.Source)
        {
            ConnectorRead();
        }
        
        if (Connector.Configuration.Direction == ConnectorDirectionEnum.Sink)
        {
            ConnectorWrite();
        }

        ExecuteExit();
    }
    
    public void Stop()
    {
        _timer.Stop();
        ConnectorDisconnect();
    }

    private bool ExecuteEnter()
    {
        if (_isExecuting)
        {
            Logger.Warn($"[{Connector.Configuration.Name}] Execution overlap.  Consider increasing scan interval.  Previous execution duration was {ExecutionDuration}ms.");
            return false;
        }
        
        _isExecuting = true;
        _executionEnter = DateTime.UtcNow.ToEpochMilliseconds();

        Connector.BeforeUpdate();
        
        return true;
    }

    private void ExecuteExit()
    {
        _isExecuting = false;
        _executionExit = DateTime.UtcNow.ToEpochMilliseconds();
        ExecutionDuration = _executionExit - _executionEnter;
        
        Connector.AfterUpdate();
    }

    private void StartTimer()
    {
        _timer = new Timer();
        _timer.Elapsed += (_, _) => { Execute(); };
        _timer.Interval = Connector.Configuration.ScanIntervalMs;
        _timer.Enabled = true;
    }
    
    private bool ConnectorInitialize()
    {
        if (Connector.Initialize(this))
        {
            Logger.Info($"[{Connector.Configuration.Name}] Connector initialized.");
            return true;
        }
        else
        {
            Logger.Error(Connector.FaultReason, $"[{Connector.Configuration.Name}] Connector initialization failed.");
            return false;
        }
    }

    private bool ConnectorCreate()
    {
        if (Connector.Create())
        {
            Logger.Info($"[{Connector.Configuration.Name}] Connector created.");
            return true;
        }
        else
        {
            Logger.Error(Connector.FaultReason, $"[{Connector.Configuration.Name}] Connector creation failed.");
            return false;
        }
    }

    private bool ConnectorConnect()
    {
        if (Connector.IsConnected)
        {
            return true;
        }
        
        if (Connector.Connect())
        {
            Logger.Info($"[{Connector.Configuration.Name}] Connector connected.");
            return true;
        }
        else
        {
            Logger.Error(Connector.FaultReason, $"[{Connector.Configuration.Name}] Connector connection failed.");
            return false;
        }
    }

    private bool ConnectorRead()
    {
        if (Connector.Configuration.Direction == ConnectorDirectionEnum.Source)
        {
            if (((ISourceConnector)Connector).Read())
            {
                Logger.Info($"[{Connector.Configuration.Name}] Connector read.");
                return true;
            }
            else
            {
                Logger.Error(Connector.FaultReason, $"[{Connector.Configuration.Name}] Connector reading failed.");
                return false;
            }
        }

        return true;
    }
    
    private bool ConnectorWrite()
    {
        if (Connector.Configuration.Direction == ConnectorDirectionEnum.Sink)
        {
            if (((ISinkConnector)Connector).Write())
            {
                Logger.Info($"[{Connector.Configuration.Name}] Connector written.");
                return true;
            }
            else
            {
                Logger.Error(Connector.FaultReason, $"[{Connector.Configuration.Name}] Connector writing failed.");
                return false;
            }
        }

        return true;
    }

    private bool ConnectorDisconnect()
    {
        if (Connector.Disconnect())
        {
            Logger.Info($"[{Connector.Configuration.Name}] Connector disconnected.");
            return true;
        }
        else
        {
            Logger.Error(Connector.FaultReason, $"[{Connector.Configuration.Name}] Connector disconnection failed.");
            return false;
        }
    }
}