using System.Collections.Concurrent;
using IDS.Transporter.Configuration;

namespace IDS.Transporter.Connectors;

public interface IConnector
{
    public IConnectorConfiguration Configuration { get; }
    public Exception FaultReason { get; }
    public ConcurrentBag<ReadResponse> DeltaReadResponses { get; }
    public bool IsFaulted { get; }
    public bool IsConnected { get; }
    public bool Initialize();
    public bool Create();
    public bool Connect();
    public bool Read();
    public bool Write();
    public bool Disconnect();
}