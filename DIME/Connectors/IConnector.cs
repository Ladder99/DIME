using System.Collections.Concurrent;
using DIME.Configuration;

namespace DIME.Connectors;

public interface ISourceConnector: IConnector
{
    public List<MessageBoxMessage> Inbox { get; }
    public List<MessageBoxMessage> Samples { get; }
    public Dictionary<string, MessageBoxMessage> Current { get; }
    public Dictionary<string, MessageBoxMessage> UserCache { get; }
    public Dictionary<string, MessageBoxMessage> TagValues { get; }
    public bool Read();
    public event Action<List<MessageBoxMessage>, Dictionary<string, MessageBoxMessage>, List<MessageBoxMessage>> OnInboxReady;
    public event Action<List<MessageBoxMessage>, Dictionary<string, MessageBoxMessage>, List<MessageBoxMessage>> OnInboxSent;
}

public interface ISinkConnector: IConnector
{
    public List<MessageBoxMessage> Outbox { get; }
    public bool IsWriting { get; }
    public bool Write();
    public event Action<List<MessageBoxMessage>> OnOutboxReady;
    public event Action<List<MessageBoxMessage>, bool> OnOutboxSent;
}

public interface IConnector
{
    public ConnectorRunner Runner { get; }
    public IConnectorConfiguration Configuration { get; }
    public Exception FaultReason { get; }
    public bool IsFaulted { get; }
    public bool IsConnected { get; }
    public bool Initialize(ConnectorRunner runner);
    public bool Create();
    public bool BeforeUpdate();
    public bool Connect();
    public bool AfterUpdate();
    public bool Disconnect();
    public bool Deinitialize();
    public event Action OnCreate;
    public event Action OnDestroy;
    public event Action OnConnect;
    public event Action OnDisconnect;
    public event Action<Exception> OnRaiseFault;
    public event Action<Exception> OnClearFault;
    public event Action<long, long, long> OnLoopPerf;
}