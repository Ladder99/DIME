using System.Collections.Concurrent;
using DIME.Configuration.WebsocketServer;
using Newtonsoft.Json;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace DIME.Connectors.WebsocketServer;

public class Sink: SinkConnector<ConnectorConfiguration, ConnectorItem>
{
    private class Feed : WebSocketBehavior
    {
        
    }
    
    private WebSocketServer _client;
    private ConcurrentDictionary<string, MessageBoxMessage> _messagesDictionary;
    private List<MessageBoxMessage> _messagesList;
    
    public Sink(ConnectorConfiguration configuration, Disruptor.Dsl.Disruptor<MessageBoxMessage> disruptor) : base(configuration, disruptor)
    {
    }

    protected override bool InitializeImplementation()
    {
        return true;
    }

    protected override bool CreateImplementation()
    {
        _messagesDictionary = new ConcurrentDictionary<string, MessageBoxMessage>();
        _messagesList = new List<MessageBoxMessage>();
        
        _client = new WebSocketServer(Configuration.Uri);
        _client.AddWebSocketService<Feed>("/");
        return true;
    }

    protected override bool ConnectImplementation()
    {
        _client.Start();
        return _client.IsListening;
    }

    protected override bool WriteImplementation()
    { 
        foreach (var message in Outbox)
        {
            var msg = JsonConvert.SerializeObject(message);
            var sessions = _client.WebSocketServices["/"].Sessions.Sessions;
            
            foreach (var session in sessions)
            {
                if (session.ConnectionState == WebSocketState.Open)
                {
                    session.Context.WebSocket.Send(msg);
                }
            }
        }
        
        return true;
    }

    protected override bool DisconnectImplementation()
    {
        _client.Stop();
        return true;
    }
    
    protected override bool DeinitializeImplementation()
    {
        return true;
    }
}