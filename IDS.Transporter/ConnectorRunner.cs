using System.Collections.Concurrent;
using IDS.Transporter.Configuration;
using IDS.Transporter.Connectors;
using IDS.Transporter.Connectors.Mqtt;
using NLog;
using Timer = System.Timers.Timer;

namespace IDS.Transporter;

public class ConnectorRunner: Disruptor.IEventHandler<MessageBoxMessage>
{
    protected readonly NLog.Logger Logger;
    private IConnector _connector;
    private Disruptor.Dsl.Disruptor<MessageBoxMessage> _disruptor;
    private BlockingCollection<MessageBoxMessage> _queueSubscription;
    private ManualResetEvent _exitEvents;
    private Timer _timer;
    private bool _isExecuting;
    private long _executionEnter = DateTime.UtcNow.ToEpochMilliseconds();
    private long _executionExit = DateTime.UtcNow.ToEpochMilliseconds();
    private long _executionDuration;

    public ConnectorRunner(IConnector connector, Disruptor.Dsl.Disruptor<MessageBoxMessage> disruptor)
    {
        Logger = NLog.LogManager.GetLogger(GetType().FullName);
        _connector = connector;
        _disruptor = disruptor;
    }
    
    public void Start(ManualResetEvent exitEvent)
    {
        _exitEvents = exitEvent;

        if (_connector.Configuration.Direction == ConnectorDirectionEnum.Sink)
        {
            _disruptor.HandleEventsWith(this);
        }

        ConnectorInitialize();
        ConnectorCreate();
        StartTimer();
    }

    public void OnEvent(MessageBoxMessage data, long sequence, bool endOfBatch)
    {
        (_connector as ISinkConnector).Outbox.Add(data);
    }
    
    private void Execute()
    {
        if (!ExecuteEnter())
        {
            return;
        }

        ConnectorConnect();

        if (_connector.Configuration.Direction == ConnectorDirectionEnum.Source)
        {
            ConnectorRead();
        }
        
        if (_connector.Configuration.Direction == ConnectorDirectionEnum.Sink)
        {
            ConnectorWrite();
        }

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
            Logger.Warn($"[{_connector.Configuration.Name}] Execution overlap.  Consider increasing scan interval.  Previous execution duration was {_executionDuration}ms.");
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
        _timer.Interval = _connector.Configuration.ScanInterval;
        _timer.Enabled = true;
    }
    
    private bool ConnectorInitialize()
    {
        if (_connector.Initialize())
        {
            Logger.Info($"[{_connector.Configuration.Name}] Connector initialized.");
            return true;
        }
        else
        {
            Logger.Error(_connector.FaultReason, $"[{_connector.Configuration.Name}] Connector initialization failed.");
            return false;
        }
    }

    private bool ConnectorCreate()
    {
        if (_connector.Create())
        {
            Logger.Info($"[{_connector.Configuration.Name}] Connector created.");
            return true;
        }
        else
        {
            Logger.Error(_connector.FaultReason, $"[{_connector.Configuration.Name}] Connector creation failed.");
            return false;
        }
    }

    private bool ConnectorConnect()
    {
        if (_connector.IsConnected)
        {
            return true;
        }
        
        if (_connector.Connect())
        {
            Logger.Info($"[{_connector.Configuration.Name}] Connector connected.");
            return true;
        }
        else
        {
            Logger.Error(_connector.FaultReason, $"[{_connector.Configuration.Name}] Connector connection failed.");
            return false;
        }
    }

    private bool ConnectorRead()
    {
        if (_connector.Configuration.Direction == ConnectorDirectionEnum.Source)
        {
            if ((_connector as ISourceConnector).Read())
            {
                Logger.Info($"[{_connector.Configuration.Name}] Connector read.");
                return true;
            }
            else
            {
                Logger.Error(_connector.FaultReason, $"[{_connector.Configuration.Name}] Connector reading failed.");
                return false;
            }
        }

        return true;
    }
    
    private bool ConnectorWrite()
    {
        if (_connector.Configuration.Direction == ConnectorDirectionEnum.Sink)
        {
            if ((_connector as ISinkConnector).Write())
            {
                Logger.Info($"[{_connector.Configuration.Name}] Connector written.");
                return true;
            }
            else
            {
                Logger.Error(_connector.FaultReason, $"[{_connector.Configuration.Name}] Connector writing failed.");
                return false;
            }
        }

        return true;
    }

    private bool ConnectorDisconnect()
    {
        if (_connector.Disconnect())
        {
            Logger.Info($"[{_connector.Configuration.Name}] Connector disconnected.");
            return true;
        }
        else
        {
            Logger.Error(_connector.FaultReason, $"[{_connector.Configuration.Name}] Connector disconnection failed.");
            return false;
        }
    }
}