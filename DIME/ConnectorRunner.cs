using DIME.Configuration;
using DIME.Connectors;
using Timer = System.Timers.Timer;

namespace DIME;

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
        Connector = connector;
        Runners = runners;
        _disruptor = disruptor;
        
        Logger.Trace($"[{Connector.Configuration.Name}] .ctor");
    }
    
    public void Start()
    {
        Logger.Trace($"[{Connector.Configuration.Name}] Start::ENTER");
        if (Connector.Configuration.Direction == ConnectorDirectionEnum.Sink)
        {
            _disruptor.HandleEventsWith(new SinkMessageHandler((ISinkConnector)Connector));
        }

        ConnectorInitialize();
        ConnectorCreate();
        StartTimer();
        Logger.Trace($"[{Connector.Configuration.Name}] Start::EXIT");
    }

    private void Execute()
    {
        Logger.Trace($"[{Connector.Configuration.Name}] Execute::ENTER");
        
        if (ExecuteEnter())
        {
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
        
        Logger.Trace($"[{Connector.Configuration.Name}] Execute::EXIT");
    }
    
    public void Stop()
    {
        Logger.Trace($"[{Connector.Configuration.Name}] Stop::ENTER");
        
        _timer.Stop();
        ConnectorDisconnect();
        ConnectorDeinitialize();
        
        Logger.Trace($"[{Connector.Configuration.Name}] Stop::EXIT");
    }

    private bool ExecuteEnter()
    {
        Logger.Trace($"[{Connector.Configuration.Name}] ExecuteEnter::ENTER");

        bool result = false;
        
        if (_isExecuting)
        {
            Logger.Warn($"[{Connector.Configuration.Name}] Execution overlap.  Consider increasing scan interval.  Previous execution duration was {ExecutionDuration}ms.");

            result = false;
        }
        else
        {
            _isExecuting = true;
            _executionEnter = DateTime.UtcNow.ToEpochMilliseconds();

            Connector.BeforeUpdate();
        
            result = true;
        }
        
        Logger.Trace($"[{Connector.Configuration.Name}] ExecuteEnter::EXIT");

        return result;
    }

    private void ExecuteExit()
    {
        Logger.Trace($"[{Connector.Configuration.Name}] ExecuteExit::ENTER");
        
        Connector.AfterUpdate();
        
        _executionExit = DateTime.UtcNow.ToEpochMilliseconds();
        ExecutionDuration = _executionExit - _executionEnter;
        _isExecuting = false;
        
        Logger.Trace($"[{Connector.Configuration.Name}] ExecuteExit::EXIT");
    }

    private void StartTimer()
    {
        Logger.Trace($"[{Connector.Configuration.Name}] StartTimer::ENTER");
        
        _timer = new Timer();
        _timer.Elapsed += (_, _) => { Execute(); };
        _timer.Interval = Connector.Configuration.ScanIntervalMs;
        _timer.Enabled = true;
        
        Logger.Trace($"[{Connector.Configuration.Name}] StartTimer::EXIT");
    }
    
    private bool ConnectorInitialize()
    {
        Logger.Trace($"[{Connector.Configuration.Name}] ConnectorInitialize::ENTER");
        
        bool result = false;
        
        if (Connector.Initialize(this))
        {
            Logger.Info($"[{Connector.Configuration.Name}] Connector initialized.");
            result = true;
        }
        else
        {
            Logger.Error(Connector.FaultReason, $"[{Connector.Configuration.Name}] Connector initialization failed.");
            result = false;
        }
        
        Logger.Trace($"[{Connector.Configuration.Name}] ConnectorInitialize::EXIT");
        
        return result;
    }

    private bool ConnectorCreate()
    {
        Logger.Trace($"[{Connector.Configuration.Name}] ConnectorCreate::ENTER");

        bool result = false;
        
        if (Connector.Create())
        {
            Logger.Info($"[{Connector.Configuration.Name}] Connector created.");
            result = true;
        }
        else
        {
            Logger.Error(Connector.FaultReason, $"[{Connector.Configuration.Name}] Connector creation failed.");
            result = false;
        }
        
        Logger.Trace($"[{Connector.Configuration.Name}] ConnectorCreate::EXIT");
        
        return result;
    }

    private bool ConnectorConnect()
    {
        Logger.Trace($"[{Connector.Configuration.Name}] ConnectorConnect::ENTER");
        
        bool result = false;
        
        if (Connector.IsConnected)
        {
            result = true;
        }
        else
        {
            if (Connector.Connect())
            {
                Logger.Info($"[{Connector.Configuration.Name}] Connector connected.");
                result = true;
            }
            else
            {
                Logger.Error(Connector.FaultReason, $"[{Connector.Configuration.Name}] Connector connection failed.");
                result = false;
            }
        }
        
        Logger.Trace($"[{Connector.Configuration.Name}] ConnectorConnect::EXIT");
        
        return result;
    }

    private bool ConnectorRead()
    {
        Logger.Trace($"[{Connector.Configuration.Name}] ConnectorRead::ENTER");
        
        bool result = true;
        
        if (Connector.Configuration.Direction == ConnectorDirectionEnum.Source)
        {
            if (((ISourceConnector)Connector).Read())
            {
                Logger.Info($"[{Connector.Configuration.Name}] Connector read.");
                result = true;
            }
            else
            {
                Logger.Error(Connector.FaultReason, $"[{Connector.Configuration.Name}] Connector reading failed.");
                result = false;
            }
        }

        Logger.Trace($"[{Connector.Configuration.Name}] ConnectorRead::EXIT");
        
        return result;
    }
    
    private bool ConnectorWrite()
    {
        Logger.Trace($"[{Connector.Configuration.Name}] ConnectorWrite::ENTER");
        
        bool result = true;
        
        if (Connector.Configuration.Direction == ConnectorDirectionEnum.Sink)
        {
            if (((ISinkConnector)Connector).Write())
            {
                Logger.Info($"[{Connector.Configuration.Name}] Connector written.");
                result = true;
            }
            else
            {
                Logger.Error(Connector.FaultReason, $"[{Connector.Configuration.Name}] Connector writing failed.");
                result = false;
            }
        }

        Logger.Trace($"[{Connector.Configuration.Name}] ConnectorWrite::EXIT");
        
        return result;
    }

    private bool ConnectorDisconnect()
    {
        Logger.Trace($"[{Connector.Configuration.Name}] ConnectorDisconnect::ENTER");
        
        bool result = false;
        
        if (Connector.Disconnect())
        {
            Logger.Info($"[{Connector.Configuration.Name}] Connector disconnected.");
            result = true;
        }
        else
        {
            Logger.Error(Connector.FaultReason, $"[{Connector.Configuration.Name}] Connector disconnection failed.");
            result = false;
        }
        
        Logger.Trace($"[{Connector.Configuration.Name}] ConnectorDisconnect::EXIT");

        return result;
    }
    
    private bool ConnectorDeinitialize()
    {
        Logger.Trace($"[{Connector.Configuration.Name}] ConnectorDeinitialize::ENTER");
        
        bool result = false;
        
        if (Connector.Deinitialize())
        {
            Logger.Info($"[{Connector.Configuration.Name}] Connector deinitialized.");
            result = true;
        }
        else
        {
            Logger.Error(Connector.FaultReason, $"[{Connector.Configuration.Name}] Connector deinitialization failed.");
            result = false;
        }
        
        Logger.Trace($"[{Connector.Configuration.Name}] ConnectorDeinitialize::EXIT");

        return result;
    }
}