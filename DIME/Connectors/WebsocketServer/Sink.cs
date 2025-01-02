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
    
    public Sink(ConnectorConfiguration configuration, Disruptor.Dsl.Disruptor<MessageBoxMessage> disruptor) : base(configuration, disruptor)
    {
    }

    protected override bool InitializeImplementation()
    {
        return true;
    }

    protected override bool CreateImplementation()
    {
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
            var sessions = _client.WebSocketServices["/"].Sessions.Sessions;
            
            foreach (var session in sessions)
            {
                if (session.ConnectionState == WebSocketState.Open)
                {
                    session.Context.WebSocket.Send(Configuration.UseSinkTransform ? TransformAndSerializeMessage(message) : JsonConvert.SerializeObject(message));
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