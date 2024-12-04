using System.Collections.Concurrent;
using System.Net;
using System.Text;
using DIME.Configuration.HttpServer;
using Newtonsoft.Json;

namespace DIME.Connectors.HttpServer;

public class Sink: SinkConnector<ConnectorConfiguration, Configuration.ConnectorItem>
{
    private HttpListener _listener;
    private ConcurrentDictionary<string, MessageBoxMessage> _messages;
    
    public Sink(ConnectorConfiguration configuration, Disruptor.Dsl.Disruptor<MessageBoxMessage> disruptor) : base(configuration, disruptor)
    {
    }

    protected override bool InitializeImplementation()
    {
        return true;
    }

    protected override bool CreateImplementation()
    {
        _messages = new();
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
            //Console.WriteLine($"{message.Path} = {message.Data}");
            _messages[message.Path] = message;
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
            responseString = JsonConvert.SerializeObject(_messages);
            response.StatusCode = 200;
        }
        else if (request.RawUrl.StartsWith("/items/"))
        {
            var itemPath = request.RawUrl.Replace("/items/", "");
            var itemDict = _messages.Where(x => x.Key.StartsWith(itemPath)).ToDictionary();
                
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