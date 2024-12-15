using System.Collections.Concurrent;
using System.Net;
using System.Text;
using DIME.Connectors;
using Newtonsoft.Json;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace DIME;

public class AdminServer
{
    public class LastNTracker<T>
    {
        public List<T> Items { get; }
        private readonly int capacity;

        public LastNTracker(int capacity)
        {
            this.capacity = capacity;
            this.Items = new List<T>(capacity);
        }

        public void Add(T item)
        {
            if (Items.Count == capacity)
            {
                Items.RemoveAt(0);
            }
            Items.Add(item);
        }

        public IReadOnlyList<T> GetItems()
        {
            return Items.AsReadOnly();
        }
    }

    private class TimestampMessage
    {
        public DateTime Timestamp { get; set; }
        public string Message { get; set; }
    }
    
    private class ConnectorStatus
    {
        public string Name { get; set; }
        public string Direction { get; set; }
        public string ConnectorType { get; set; }
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
        public long ConnectCount { get; set; }
        public long DisconnectCount { get; set; }
        public long FaultCount { get; set; }
        public long OutboxSendFailCount { get; set; }
        public DateTime LastUpdate { get; set; }
        public DateTime StartTime { get; set; }
        public List<string> ActiveExclusionFilters { get; set; }
        public List<string> ActiveInclusionFilters { get; set; }
        public LastNTracker<TimestampMessage> RecentErrors { get; set; }
    }
    
    private class Feed : WebSocketBehavior
    {
        
    }

    private readonly Dictionary<string, ConnectorStatus> _connectorStatuses = new Dictionary<string, ConnectorStatus>();
    private HttpListener _httpServer;
    private WebSocketServer _wsServer;
    private bool _isRunning;
    private DimeService _service;
    private IConfigurationProvider _configurationProvider;
    
    public AdminServer(DimeService service, IConfigurationProvider configurationProvider, string httpUri, string wsUri)
    {
        _service = service;
        _configurationProvider = configurationProvider;
        _httpServer = new HttpListener();
        _httpServer.Prefixes.Add(httpUri);
        _wsServer = new WebSocketServer(wsUri);
        _wsServer.AddWebSocketService<Feed>("/");
    }

    public void Start()
    {
        _isRunning = true;
        _httpServer.Start();
        BeginAcceptRequest();
        _wsServer.Start();
    }

    public void Stop()
    {
        _isRunning = false;
        _httpServer.Stop();
        _httpServer.Close();
        _wsServer.Stop();
    }
    
    private void BeginAcceptRequest()
    {
        try
        {
            _httpServer.BeginGetContext(HandleRequest, null);
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
            HttpListenerContext context = _httpServer.EndGetContext(result);
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
        try
        {
            HttpListenerRequest request = context.Request;
            HttpListenerResponse response = context.Response;

            var responseString = "{}";

            if (request.RawUrl == "/status")
            {
                responseString = JsonConvert.SerializeObject(_connectorStatuses);
                response.StatusCode = 200;
            }
            else if (request.RawUrl == "/config/yaml")
            {
                if (request.HttpMethod == "GET")
                {
                    responseString = _configurationProvider.ReadConfiguration().Item1;
                    response.StatusCode = 200;
                }

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

                    var (success, stringConfiguration, dictionaryConfiguration) = _configurationProvider.WriteConfiguration(postData);
                    responseString = stringConfiguration;
                    response.StatusCode = success ? 200 : 400;
                }
            }
            else if (request.RawUrl == "/config/json")
            {
                responseString = JsonConvert.SerializeObject(_configurationProvider.ReadConfiguration().Item2);
                response.StatusCode = 200;
            }
            else if (request.RawUrl == "/service/restart")
            {
                _service.Restart();
                responseString = "Restarted";
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
        catch (Exception e)
        {
            if (_isRunning)
            {
                BeginAcceptRequest();
            }
        }
    }

    private void BroadcastToAllWebsocketClients(ConnectorStatus connectorStatus)
    {
        var msg = JsonConvert.SerializeObject(connectorStatus);
        var sessions = _wsServer.WebSocketServices["/"]?.Sessions.Sessions;

        if (sessions is null) return;
        foreach (var session in sessions)
        {
            if (session.State == WebSocketState.Open)
            {
                session.Context.WebSocket.Send(msg);
            }
        }
    }
    
    public void OnCreate(IConnector connector)
    {
        _connectorStatuses[connector.Configuration.Name] = new ConnectorStatus()
        {
            Name = connector.Configuration.Name,
            Direction = connector.Configuration.Direction.ToString(),
            ConnectorType = connector.Configuration.ConnectorType,
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
            ConnectCount = 0,
            DisconnectCount = 0,
            FaultCount = 0,
            OutboxSendFailCount = 0,
            LastUpdate = DateTime.Now,
            StartTime = DateTime.Now,
            ActiveExclusionFilters = connector.Configuration.ExcludeFilter,
            ActiveInclusionFilters = connector.Configuration.IncludeFilter,
            RecentErrors = new LastNTracker<TimestampMessage>(10)
        };
        
        BroadcastToAllWebsocketClients(_connectorStatuses[connector.Configuration.Name]);
    }

    public void OnDestroy(IConnector connector)
    {
        _connectorStatuses[connector.Configuration.Name].LastUpdate = DateTime.Now;
        BroadcastToAllWebsocketClients(_connectorStatuses[connector.Configuration.Name]);
    }
    
    public void OnConnect(IConnector connector)
    {
        _connectorStatuses[connector.Configuration.Name].ConnectCount += 1;
        _connectorStatuses[connector.Configuration.Name].IsConnected = true;
        _connectorStatuses[connector.Configuration.Name].LastUpdate = DateTime.Now;
        BroadcastToAllWebsocketClients(_connectorStatuses[connector.Configuration.Name]);
    }
    
    public void OnDisconnect(IConnector connector)
    {
        _connectorStatuses[connector.Configuration.Name].DisconnectCount += 1;
        _connectorStatuses[connector.Configuration.Name].IsConnected = true;
        _connectorStatuses[connector.Configuration.Name].LastUpdate = DateTime.Now;
        BroadcastToAllWebsocketClients(_connectorStatuses[connector.Configuration.Name]);
    }
    
    public void OnRaiseFault(IConnector connector, Exception exception)
    {
        _connectorStatuses[connector.Configuration.Name].RecentErrors.Add(
            new TimestampMessage() { Message = exception.Message, Timestamp = DateTime.Now });
        _connectorStatuses[connector.Configuration.Name].FaultCount += 1;
        _connectorStatuses[connector.Configuration.Name].IsFaulted = true;
        _connectorStatuses[connector.Configuration.Name].FaultMessage = exception.Message;
        _connectorStatuses[connector.Configuration.Name].LastUpdate = DateTime.Now;
        BroadcastToAllWebsocketClients(_connectorStatuses[connector.Configuration.Name]);
    }

    public void OnClearFault(IConnector connector, Exception exception)
    {
        _connectorStatuses[connector.Configuration.Name].IsFaulted = false;
        _connectorStatuses[connector.Configuration.Name].FaultMessage = string.Empty;
        _connectorStatuses[connector.Configuration.Name].LastUpdate = DateTime.Now;
        BroadcastToAllWebsocketClients(_connectorStatuses[connector.Configuration.Name]);
    }

    public void OnOutboxReady(IConnector connector, ConcurrentBag<MessageBoxMessage> outbox)
    {
        _connectorStatuses[connector.Configuration.Name].MessagesAttempted += outbox.Count;
        _connectorStatuses[connector.Configuration.Name].LastUpdate = DateTime.Now;
        BroadcastToAllWebsocketClients(_connectorStatuses[connector.Configuration.Name]);
    }

    public void OnOutboxSent(IConnector connector, ConcurrentBag<MessageBoxMessage> outbox, bool success)
    {
        if (!success) _connectorStatuses[connector.Configuration.Name].OutboxSendFailCount += 1;
        _connectorStatuses[connector.Configuration.Name].MessagesAccepted += outbox.Count;
        _connectorStatuses[connector.Configuration.Name].LastUpdate = DateTime.Now;
        BroadcastToAllWebsocketClients(_connectorStatuses[connector.Configuration.Name]);
    }

    public void OnInboxReady(IConnector connector, ConcurrentBag<MessageBoxMessage> inbox, ConcurrentDictionary<string, MessageBoxMessage> current, ConcurrentBag<MessageBoxMessage> samples)
    {
        _connectorStatuses[connector.Configuration.Name].MessagesAttempted += inbox.Count;
        _connectorStatuses[connector.Configuration.Name].LastUpdate = DateTime.Now;
        BroadcastToAllWebsocketClients(_connectorStatuses[connector.Configuration.Name]);
    }
    
    public void OnInboxSent(IConnector connector, ConcurrentBag<MessageBoxMessage> inbox, ConcurrentDictionary<string, MessageBoxMessage> current, ConcurrentBag<MessageBoxMessage> samples)
    {
        _connectorStatuses[connector.Configuration.Name].MessagesAccepted += inbox.Count;
        _connectorStatuses[connector.Configuration.Name].LastUpdate = DateTime.Now;
        BroadcastToAllWebsocketClients(_connectorStatuses[connector.Configuration.Name]);
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
        BroadcastToAllWebsocketClients(_connectorStatuses[connector.Configuration.Name]);
    }
    
}