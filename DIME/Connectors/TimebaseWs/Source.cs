using System.Net.WebSockets;
using System.Text;
using System.Text.RegularExpressions;
using Timer = System.Timers.Timer;
using DIME.Configuration.TimebaseWs;
using Newtonsoft.Json;

namespace DIME.Connectors.TimebaseWs;

public class Source: QueuingSourceConnector<ConnectorConfiguration, ConnectorItem>
{
    private ClientWebSocket _client = null;
    private Timer _receiveTimer = null;
    private bool _receiveTimerExecuting = false;
    
    public Source(ConnectorConfiguration configuration, Disruptor.Dsl.Disruptor<MessageBoxMessage> disruptor) : base(configuration, disruptor)
    {
    }

    protected override bool InitializeImplementation()
    {
        Properties.SetProperty("uri",  new Uri($"ws://{Configuration.Address}:{Configuration.Port}/api/subscribe"));
        Properties.SetProperty("format_message", "{\"protocol\":\"json\", \"version\":1}");
        Properties.SetProperty("subscribe_message", "{{\"arguments\": [\"{0}\", \"{1}\"], \"target\": \"Subscribe\", \"type\": 1}}");
        return true;
    }

    protected override bool CreateImplementation()
    {
        _receiveTimer = new Timer();
        _receiveTimer.Elapsed += (_, _) => { SocketReceive(); };
        _receiveTimer.Interval = Configuration.ScanIntervalMs / 4;
        _receiveTimer.Enabled = false;
        return true;
    }
    
    protected override bool ConnectImplementation()
    {
        try
        {
            _client = new ClientWebSocket();
            _client.ConnectAsync(Properties.GetProperty<Uri>("uri"), CancellationToken.None).Wait();
            var request = Properties.GetProperty<string>("format_message");
            var response = SendAndReceiveMessage(request);
            System.Console.WriteLine($"FMT RESPONSE {request} >> {response}");

            foreach (var item in Configuration.Items)
            {
                request = string.Format(Properties.GetProperty<string>("subscribe_message"), item.Group, item.Address);
                response = SendAndReceiveMessage(request);
                System.Console.WriteLine($"SUB RESPONSE {request} >> {response}");
            }
            
            _receiveTimer.Enabled = true;
            
            return true;
        }
        catch (Exception e)
        {
            return false;
        }
    }
    
    protected override bool DisconnectImplementation()
    {
        _receiveTimer.Enabled = false;
        _client.CloseAsync(WebSocketCloseStatus.NormalClosure, "closing", CancellationToken.None).Wait();
        return true;
    }
    
    protected override bool DeinitializeImplementation()
    {
        return true;
    }

    private string SendAndReceiveMessage(string message)
    {
        var sendBuffer = Encoding.UTF8.GetBytes($"{message}\u001e");
        var receiveBuffer = new byte[1024];
        _client.SendAsync(new ArraySegment<byte>(sendBuffer), WebSocketMessageType.Text, true, CancellationToken.None ).Wait();
        var receiveResult = _client.ReceiveAsync(new ArraySegment<byte>(receiveBuffer), CancellationToken.None).GetAwaiter().GetResult();
        var receivedMessage = Encoding.UTF8.GetString(receiveBuffer, 0, receiveResult.Count);
        return receivedMessage;
    }
    
    private void SocketReceive()
    {
        if (_receiveTimerExecuting)
        {
            System.Console.WriteLine("TIMER OVERLAP");
            return;
        }
        
        //Console.WriteLine("TIMER ENTER");
        
        _receiveTimerExecuting = true;

        var buffer = new byte[1024 * 4];
        if (_client.State == WebSocketState.Open)
        {
            SubscriptionResultPayload payload = null;

            try
            {
                var result = _client.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None).GetAwaiter()
                    .GetResult();
                var message = System.Text.Encoding.UTF8.GetString(buffer, 0, result.Count);
                System.Console.WriteLine($"RECEIVED {message}");

                payload = JsonConvert.DeserializeObject<SubscriptionResultPayload>(Regex.Replace(message, @"\p{C}", ""));
                var tagDataset = payload.Arguments[0].Dataset;
                var tagName = payload.Arguments[0].TagName;
                var tagValue = payload.Arguments[0].Tvq.Value;
                var tagTimestamp = payload.Arguments[0].Tvq.Timestamp;

                try
                {
                    var item = Configuration.Items.First(x => x.Group == tagDataset && x.Address == tagName);
                    AddToIncomingBuffer(item.Name, tagValue, tagTimestamp.ToEpochMilliseconds());
                }
                catch (InvalidOperationException e)
                {
                    System.Console.WriteLine($"ITEM NF {tagDataset}/{tagName}");
                }
            }
            catch (WebSocketException we)
            {
                IsConnected = false;
            }
            catch (Exception e)
            {
                
            }
        }
        else
        {
            System.Console.WriteLine($"RECV STATE {_client.State}");
        }

        _receiveTimerExecuting = false;
        _receiveTimer.Enabled = true;
        
        //Console.WriteLine("TIMER EXIT");
    }

    public class SubscriptionResultPayload
    {
        [JsonProperty("type")]
        public int Type { get; set; }

        [JsonProperty("target")]
        public string Target { get; set; }

        [JsonProperty("arguments")]
        public Argument[] Arguments { get; set; }

        public class Argument
        {
            [JsonProperty("dataset")]
            public string Dataset { get; set; }

            [JsonProperty("tagname")]
            public string TagName { get; set; }

            [JsonProperty("tvq")]
            public TimeValueQuality Tvq { get; set; }
        }

        public class TimeValueQuality
        {
            [JsonProperty("t")]
            public DateTime Timestamp { get; set; }

            [JsonProperty("v")]
            public string Value { get; set; }

            [JsonProperty("q")]
            public int Quality { get; set; }
        }
    }
}