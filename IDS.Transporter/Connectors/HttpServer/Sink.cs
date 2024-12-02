using System.Net;
using System.Text;
using IDS.Transporter.Configuration.HttpServer;
using Newtonsoft.Json;

namespace IDS.Transporter.Connectors.HttpServer;

public class Sink: SinkConnector<ConnectorConfiguration, ConnectorItem>
{
    private HttpListener _listener;
    private bool _isRunning;
    private Dictionary<string, MessageBoxMessage> _messages;
    
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
        _listener.Start();
        _isRunning = true;
        BeginAcceptRequest();
        return true;
    }

    protected override bool ConnectImplementation()
    {
        IsConnected = true;
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
        _isRunning = false;
        _listener.Stop();
        _listener.Close();
        return true;
    }
    
    private void BeginAcceptRequest()
    {
        if (!_isRunning) return;

        try
        {
            // Use HttpListener.BeginGetContext to start an asynchronous operation
            _listener.BeginGetContext(HandleRequest, null);
        }
        catch (Exception ex)
        {
            if (_isRunning)
            {
                Console.WriteLine($"Error accepting request: {ex.Message}");
                BeginAcceptRequest(); // Try again
            }
        }
    }
    
    private void HandleRequest(IAsyncResult result)
    {
        try
        {
            // Complete the context retrieval
            HttpListenerContext context = _listener.EndGetContext(result);

            // Allow the server to continue accepting new requests immediately
            BeginAcceptRequest();

            // Process the request using thread pool
            ThreadPool.QueueUserWorkItem(state => ProcessRequest(context));
        }
        catch (Exception ex)
        {
            if (_isRunning)
            {
                Console.WriteLine($"Error handling request: {ex.Message}");
                BeginAcceptRequest(); // Try again
            }
        }
    }
    
    private void ProcessRequest(HttpListenerContext context)
    {
        try
        {
            HttpListenerRequest request = context.Request;
            HttpListenerResponse response = context.Response;

            var responseString = "{}";
            
            if (_messages.ContainsKey(request.RawUrl.Substring(1)))
            {
                responseString = JsonConvert.SerializeObject(_messages[request.RawUrl.Substring(1)]);
                response.StatusCode = 200;
            }
            else
            {
                response.StatusCode = 404;
            }
            
            byte[] buffer = Encoding.UTF8.GetBytes(responseString);
            response.ContentType = "application/json";
            response.ContentLength64 = buffer.Length;
            
            using (var output = response.OutputStream)
            {
                output.Write(buffer, 0, buffer.Length);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Request processing error: {ex.Message}");
        }
    }

    public void Stop()
    {
        if (!_isRunning) return;

        _isRunning = false;
        _listener.Stop();
        _listener.Close();
        Console.WriteLine("Server stopped.");
    }
}