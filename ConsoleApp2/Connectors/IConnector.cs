using ConsoleApp2.Configuration;

namespace ConsoleApp2.Connectors;

public interface IConnector
{
    //public ConnectorConfiguration<ConnectorItem> Configuration { get; }
    public bool IsFaulted { get; }
    public bool IsConnected { get; }
    public bool Initialize();
    public bool Create();
    public bool Connect();
    public bool Read();
    //public bool Write();
    public bool Disconnect();
}