using System.Collections.Concurrent;
using System.Net;
using System.Text;
using DIME.Configuration;
using DIME.Connectors;
using Newtonsoft.Json;

namespace DIME;

public class HttpServer
{
    private class ConnectorStatus
    {
        public string Name { get; set; }
        public string Direction { get; set; }
        public bool IsConnected { get; set; }
        public bool IsFaulted { get; set; }
        public string FaultMessage { get; set; }
        public long MessagesAttempted { get; set; }
        public long MessagesAccepted { get; set; }
        public long MinimumReadMs { get; set; }
        public long MaximumReadMs { get; set; }
        public long LastReadMs { get; set; }
        public long MinimumScriptMs { get; set; }
        public long MaximumScriptMs { get; set; }
        public long LastScriptMs { get; set; }
        public long MinimumLoopMs { get; set; }
        public long MaximumLoopMs { get; set; }
        public long LastLoopMs { get; set; }
        public long LoopCount { get; set; }
        public DateTime LastUpdate { get; set; }
        public DateTime StartTime { get; set; }
        public List<string> ActiveExclusionFilters { get; set; }
        public List<string> ActiveInclusionFilters { get; set; }
    }

    private Dictionary<string, ConnectorStatus> _connectorStatuses;
    private HttpListener _listener;
    private bool _isRunning;
    
    public HttpServer(string uri)
    {
        _connectorStatuses = new Dictionary<string, ConnectorStatus>();
        _listener = new HttpListener();
        _listener.Prefixes.Add(uri);
    }

    public void Start()
    {
        _isRunning = true;
        _listener.Start();
        BeginAcceptRequest();
    }

    public void Stop()
    {
        _isRunning = false;
        _listener.Stop();
        _listener.Close();
    }
    
    private void BeginAcceptRequest()
    {
        try
        {
            _listener.BeginGetContext(HandleRequest, null);
        }
        catch (Exception e)
        {
            if (_isRunning)
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
            if (_isRunning)
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

        if (request.RawUrl == "/stats")
        {
            responseString = JsonConvert.SerializeObject(_connectorStatuses);
            response.StatusCode = 200;
        }

        byte[] buffer = Encoding.UTF8.GetBytes(responseString);
        response.ContentType = "application/json";
        response.ContentLength64 = buffer.Length;
            
        using (var output = response.OutputStream)
        {
            output.Write(buffer, 0, buffer.Length);
        }
    }

    public void OnCreate(IConnector connector)
    {
        _connectorStatuses[connector.Configuration.Name] = new ConnectorStatus()
        {
            Name = connector.Configuration.Name,
            Direction = connector.Configuration.Direction.ToString(),
            IsFaulted = false,
            FaultMessage = string.Empty,
            MessagesAccepted = 0,
            MinimumLoopMs = 0,
            MinimumReadMs = 0,
            MinimumScriptMs = 0,
            MaximumScriptMs = 0,
            MaximumReadMs = 0,
            MaximumLoopMs = 0,
            LoopCount = 0,
            LastUpdate = DateTime.Now,
            StartTime = DateTime.Now,
            ActiveExclusionFilters = connector.Configuration.ExcludeFilter,
            ActiveInclusionFilters = connector.Configuration.IncludeFilter
        };
    }

    public void OnDestroy(IConnector connector)
    {
        _connectorStatuses[connector.Configuration.Name].LastUpdate = DateTime.Now;
    }
    
    public void OnConnect(IConnector connector)
    {
        _connectorStatuses[connector.Configuration.Name].IsConnected = true;
        _connectorStatuses[connector.Configuration.Name].LastUpdate = DateTime.Now;
    }
    
    public void OnDisconnect(IConnector connector)
    {
        _connectorStatuses[connector.Configuration.Name].IsConnected = true;
        _connectorStatuses[connector.Configuration.Name].LastUpdate = DateTime.Now;
    }
    
    public void OnRaiseFault(IConnector connector, Exception exception)
    {
        _connectorStatuses[connector.Configuration.Name].IsFaulted = true;
        _connectorStatuses[connector.Configuration.Name].FaultMessage = exception.Message;
        _connectorStatuses[connector.Configuration.Name].LastUpdate = DateTime.Now;
    }

    public void OnClearFault(IConnector connector, Exception exception)
    {
        _connectorStatuses[connector.Configuration.Name].IsFaulted = false;
        _connectorStatuses[connector.Configuration.Name].FaultMessage = string.Empty;
        _connectorStatuses[connector.Configuration.Name].LastUpdate = DateTime.Now;
    }

    public void OnOutboxReady(IConnector connector, ConcurrentBag<MessageBoxMessage> outbox)
    {
        _connectorStatuses[connector.Configuration.Name].MessagesAttempted += outbox.Count;
        _connectorStatuses[connector.Configuration.Name].LastUpdate = DateTime.Now;
    }

    public void OnOutboxSent(IConnector connector, ConcurrentBag<MessageBoxMessage> outbox, bool success)
    {
        _connectorStatuses[connector.Configuration.Name].MessagesAccepted += outbox.Count;
        _connectorStatuses[connector.Configuration.Name].LastUpdate = DateTime.Now;
    }

    public void OnInboxReady(IConnector connector, ConcurrentBag<MessageBoxMessage> inbox, ConcurrentDictionary<string, MessageBoxMessage> current, ConcurrentBag<MessageBoxMessage> samples)
    {
        _connectorStatuses[connector.Configuration.Name].MessagesAttempted += inbox.Count;
        _connectorStatuses[connector.Configuration.Name].LastUpdate = DateTime.Now;
    }
    
    public void OnInboxSent(IConnector connector, ConcurrentBag<MessageBoxMessage> inbox, ConcurrentDictionary<string, MessageBoxMessage> current, ConcurrentBag<MessageBoxMessage> samples)
    {
        _connectorStatuses[connector.Configuration.Name].MessagesAccepted += inbox.Count;
        _connectorStatuses[connector.Configuration.Name].LastUpdate = DateTime.Now;
    }
    
    public void OnLoopPerf(IConnector connector, long readMs, long scriptMs, long loopMs)
    {
        var status = _connectorStatuses[connector.Configuration.Name];

        if (status.LoopCount == 0)
        {
            status.MinimumReadMs = readMs;
            status.MinimumScriptMs = scriptMs;
            status.MinimumLoopMs = loopMs;
            status.MaximumReadMs = readMs;
            status.MaximumScriptMs = scriptMs;
            status.MaximumLoopMs = loopMs;
            status.LastReadMs = readMs;
            status.LastScriptMs = scriptMs;
            status.LastLoopMs = loopMs;
        }
        else
        {
            if (readMs < status.MinimumReadMs) status.MinimumReadMs = readMs;
            if (scriptMs < status.MinimumScriptMs) status.MinimumScriptMs = scriptMs;
            if (loopMs < status.MinimumLoopMs) status.MinimumLoopMs = loopMs;
            if (readMs > status.MaximumReadMs) status.MaximumReadMs = readMs;
            if (scriptMs > status.MaximumScriptMs) status.MaximumScriptMs = scriptMs;
            if (loopMs > status.MaximumLoopMs) status.MaximumLoopMs = loopMs;
            status.LastReadMs = readMs;
            status.LastScriptMs = scriptMs;
            status.LastLoopMs = loopMs;
        }

        status.LoopCount += 1;
        status.LastUpdate = DateTime.Now;

        _connectorStatuses[connector.Configuration.Name] = status;
    }
    
}