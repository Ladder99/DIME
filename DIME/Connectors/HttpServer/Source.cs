using System.Net;
using System.Text;
using DIME.Configuration.HttpServer;
using Newtonsoft.Json;

namespace DIME.Connectors.HttpServer;

public class Source: QueuingSourceConnector<ConnectorConfiguration, ConnectorItem>
{
    private HttpListener _listener;
    
    public Source(ConnectorConfiguration configuration, Disruptor.Dsl.Disruptor<MessageBoxMessage> disruptor) : base(configuration, disruptor)
    {
    }

    protected override bool InitializeImplementation()
    {
        return true;
    }

    protected override bool CreateImplementation()
    {
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
    
    protected override bool DisconnectImplementation()
    {
        _listener.Stop();
        return true;
    }
    
    protected override bool DeinitializeImplementation()
    {
        _listener.Close();
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

        if (request.HttpMethod == "POST")
        {
            string postData = "";
            
            using (Stream body = request.InputStream)
            {
                using (StreamReader reader = new StreamReader(body, request.ContentEncoding))
                {
                    postData = reader.ReadToEnd();
                }
            }
            
            _incomingBuffer.Add(new IncomingMessage()
            {
                Key = request.RawUrl.Substring(1),
                Value = postData,
                Timestamp = DateTime.UtcNow.ToEpochMilliseconds()
            });
            
            response.StatusCode = 201;
        }
        else
        {
            response.StatusCode = 400;
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