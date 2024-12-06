using System.Collections.Concurrent;
using System.Net;
using System.Text;
using DIME.Configuration.HttpServer;
using Newtonsoft.Json;

namespace DIME.Connectors.HttpServer;

public class Sink: SinkConnector<ConnectorConfiguration, ConnectorItem>
{
    private HttpListener _listener;
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
        
        _listener = new HttpListener();
        _listener.Prefixes.Add(Configuration.Uri);
        return true;
    }

    protected override bool ConnectImplementation()
    {
        _listener.Start();
        BeginAcceptRequest();
        return true;
    }

    protected override bool WriteImplementation()
    { 
        foreach (var message in Outbox)
        {
            // TODO: WHY!
            var tempMessage = new MessageBoxMessage()
            {
                Path = message.Path,
                Data = message.Data,
                Timestamp = message.Timestamp,
                ConnectorItemRef = message.ConnectorItemRef
            };
            
            _messagesDictionary.AddOrUpdate(tempMessage.Path, tempMessage, (key, oldValue) => tempMessage);
            
            var foundMessage = _messagesList.Find(x => x.Path == tempMessage.Path);
            if (foundMessage is null)
            {
                _messagesList.Add(tempMessage);
            }
            else
            {
                foundMessage.Path = tempMessage.Path;
                foundMessage.Data = tempMessage.Data;
                foundMessage.Timestamp = tempMessage.Timestamp;
                foundMessage.ConnectorItemRef = tempMessage.ConnectorItemRef;
            }
        }
        
        return true;
    }

    protected override bool DisconnectImplementation()
    {
        _listener.Stop();
        _listener.Close();
        return true;
    }
    
    protected override bool DeinitializeImplementation()
    {
        return true;
    }
    
    private void BeginAcceptRequest()
    {
        try
        {
            _listener.BeginGetContext(HandleRequest, null);
        }
        catch (Exception e)
        {
            if (IsConnected)
            {
                BeginAcceptRequest();
            }
        }
    }
    
    private void HandleRequest(IAsyncResult result)
    {
        try
        {
            HttpListenerContext context = _listener.EndGetContext(result);
            BeginAcceptRequest();
            ThreadPool.QueueUserWorkItem(state => ProcessRequest(context));
        }
        catch (Exception e)
        {
            if (IsConnected)
            {
                BeginAcceptRequest();
            }
        }
    }
    
    private void ProcessRequest(HttpListenerContext context)
    {
        HttpListenerRequest request = context.Request;
        HttpListenerResponse response = context.Response;

        var responseString = "{}";

        if (request.RawUrl == "/items")
        {
            responseString = JsonConvert.SerializeObject(_messagesDictionary);
            response.StatusCode = 200;
        }
        if (request.RawUrl == "/list")
        {
            responseString = JsonConvert.SerializeObject(_messagesList);
            response.StatusCode = 200;
        }
        else if (request.RawUrl.StartsWith("/items/"))
        {
            var itemPath = request.RawUrl.Replace("/items/", "");
            var itemDict = _messagesDictionary.Where(x => x.Key.StartsWith(itemPath)).ToDictionary();

            if (itemDict.Any())
            {
                responseString = JsonConvert.SerializeObject(itemDict);
                response.StatusCode = 200;
            }
            else
            {
                response.StatusCode = 404;
            }
        }

        byte[] buffer = Encoding.UTF8.GetBytes(responseString);
        response.ContentType = "application/json";
        response.ContentLength64 = buffer.Length;
            
        using (var output = response.OutputStream)
        {
            output.Write(buffer, 0, buffer.Length);
        }
    }
}