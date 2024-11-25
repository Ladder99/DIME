using System.Collections.Concurrent;
using IDS.Transporter.Configuration;

namespace IDS.Transporter.Connectors;

public interface ISourceConnector: IConnector
{
    public ConcurrentBag<BoxMessage> Inbox { get; }
    public bool Read();
}

public interface ISinkConnector: IConnector
{
    public ConcurrentBag<BoxMessage> Outbox { get; }
    public bool Write();
}

public interface IConnector
{
    public IConnectorConfiguration Configuration { get; }
    public Exception FaultReason { get; }
    public bool IsFaulted { get; }
    public bool IsConnected { get; }
    public bool Initialize();
    public bool Create();
    public bool Connect();
    public bool Disconnect();
}