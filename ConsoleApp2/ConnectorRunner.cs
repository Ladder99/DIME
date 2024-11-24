using ConsoleApp2.Connectors;
using ConsoleApp2.Connectors.Mqtt;
using NLog;
using Timer = System.Timers.Timer;

namespace ConsoleApp2;

public class ConnectorRunner
{
    protected readonly NLog.Logger Logger;
    private IConnector _connector;
    private ManualResetEvent _exitEvents;
    private Timer _timer;
    private bool _isExecuting;
    private long _executionEnter;
    private long _executionExit;
    private long _executionDuration;

    public ConnectorRunner(IConnector connector)
    {
        Logger = NLog.LogManager.GetLogger(GetType().FullName);
        _connector = connector;
    }
    
    public void Start(ManualResetEvent exitEvent)
    {
        _exitEvents = exitEvent;

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
        ConnectorRead();
        ExecuteExit();
    }

    public void Stop()
    {
        _timer.Stop();
        ConnectorDisconnect();
        _connector.Disconnect();
    }

    private bool ExecuteEnter()
    {
        if (_isExecuting)
        {
            Logger.Warn($"Execution overlap.  Consider increasing scan interval.  Previous execution duration was {_executionDuration}ms.");
            return false;
        }
        
        _isExecuting = true;
        _executionEnter = DateTime.UtcNow.ToEpochMilliseconds();
        return true;
    }

    private void ExecuteExit()
    {
        _isExecuting = false;
        _executionExit = DateTime.UtcNow.ToEpochMilliseconds();
        _executionDuration = _executionExit - _executionEnter;
    }

    private void StartTimer()
    {
        _timer = new Timer();
        _timer.Elapsed += (sender, args) => { Execute(); };
        _timer.Interval = 1000;// _connector.Configuration.ScanInterval;
        _timer.Enabled = true;
    }
    
    private bool ConnectorInitialize()
    {
        return _connector.Initialize();
    }

    private bool ConnectorCreate()
    {
        return _connector.Create();
    }

    private bool ConnectorConnect()
    {
        if (_connector.IsConnected)
        {
            return true;
        }
        return _connector.Connect();
    }

    private bool ConnectorRead()
    {
        return _connector.Read();
    }

    private bool ConnectorDisconnect()
    {
        return _connector.Disconnect();
    }
}