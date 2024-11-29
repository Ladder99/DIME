using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using IDS.Transporter.Configuration.HaasShdr;
using Newtonsoft.Json;
using Timer = System.Timers.Timer;

namespace IDS.Transporter.Connectors.HaasShdr;

public class Source: SourceConnector<ConnectorConfiguration, ConnectorItem>
{
    private class IncomingMessage
    {
        public string Key { get; set; }
        public string Value { get; set; }
        public long Timestamp { get; set; }
    }
    
    // connection
    private TcpClient _client = null;
    private NetworkStream _stream = null;
    private StreamWriter _writer = null;
    private ASCIIEncoding _encoding = new();
    // time since last heartbeat was sent
    private long _lastHeartbeatTs = 0;
    // how long has it been since we've seen data on the socket?
    // this triggers a socket write to turn shdr on
    private int _durationWithoutData = 0;
    // shdr fetch execution
    private Timer _timer = null;
    private bool _isReading = false;
    // shdr processor
    private long _packetCounter = 0;
    private int _lineCounter = 0;
    private string _lastLineFromPreviousPacket = string.Empty;
    // message hold
    //private readonly Queue<IncomingMessage> _incomingBuffer = new();
    private readonly ConcurrentBag<IncomingMessage> _incomingBuffer = new();
    private readonly object _incomingBufferLock = new();
    
    public Source(ConnectorConfiguration configuration, Disruptor.Dsl.Disruptor<MessageBoxMessage> disruptor) : base(configuration, disruptor)
    {
    }

    protected override bool InitializeImplementation()
    {
        return true;
    }

    protected override bool CreateImplementation()
    {
        return true;
    }

    protected override bool ConnectImplementation()
    {
        _client = new TcpClient();
        //tcpClient.NoDelay = true;
        var task = _client.ConnectAsync(
            Configuration.IpAddress,
            Configuration.Port
        );
        
        task.Wait(Configuration.TimeoutMs);

        if (_client.Connected)
        {
            // connected
            _stream = _client.GetStream();
            _writer = new StreamWriter(_stream);
            if (!ShdrOn())
            {
                // socket write failed
                return false;
            }

            _timer = new Timer();
            _timer.Elapsed += (sender, args) => { Fetch(); };
            _timer.Interval = Configuration.ScanIntervalMs;
            _timer.Enabled = true;
        }
        else
        {
            // failed to connect
            return false;
        }
        
        return true;
    }

    protected override bool ReadImplementation()
    {
        if (Configuration.ItemizedRead)
        {
            lock (_incomingBufferLock)
            {
                foreach (var item in Configuration.Items.Where(x => x.Enabled))
                {
                    IEnumerable<IncomingMessage> messages = null;

                    if (item.Address != null)
                    {
                        messages = _incomingBuffer.Where(x => x.Key == item.Address);

                        foreach (var message in messages)
                        {
                            Samples.Add(new MessageBoxMessage()
                            {
                                Path = $"{Configuration.Name}/{message.Key}",
                                Data = item.Script == null ? message.Value : ExecuteScript(message.Value, item.Script),
                                Timestamp = DateTime.UtcNow.ToEpochMilliseconds()
                            });
                        }
                    }
                    else if (item.Script != null)
                    {
                        Samples.Add(new MessageBoxMessage()
                        {
                            Path = $"{Configuration.Name}/{item.Name}",
                            Data = ExecuteScript(null, item.Script),
                            Timestamp = DateTime.UtcNow.ToEpochMilliseconds()
                        });
                    }
                }

                _incomingBuffer.Clear();
            }
        }
        else
        {
            lock (_incomingBufferLock)
            {
                foreach (var message in _incomingBuffer.ToArray())
                {
                    try
                    {
                        var item = Configuration.Items
                            .First(x => x.Enabled && x.Address == message.Key && x.Script != null);
                        Samples.Add(new MessageBoxMessage()
                        {
                            Path = $"{Configuration.Name}/{message.Key}",
                            Data = ExecuteScript(message.Value, item.Script),
                            Timestamp = message.Timestamp
                        });
                    }
                    catch (InvalidOperationException e)
                    {
                        Samples.Add(new MessageBoxMessage()
                        {
                            Path = $"{Configuration.Name}/{message.Key}",
                            Data = message.Value,
                            Timestamp = message.Timestamp
                        });
                    }
                }

                _incomingBuffer.Clear();
            }
        }

        return true;
    }

    protected override bool DisconnectImplementation()
    {
        try
        {
            _timer?.Stop();
            _lastHeartbeatTs = 0;
            ShdrOff();
            _writer?.Close();
            _stream?.Close();
            _client?.Close();
        }
        catch(Exception e)
        {
            Logger.Error(e, $"[{Configuration.Name}] Error closing connection.");
        }
        
        return true;
    }
    
    private bool ShdrOn()
    {
        try
        {
            Logger.Info($"[{Configuration.Name}] SHDR enable request.");
            _lastHeartbeatTs = DateTime.UtcNow.ToEpochMilliseconds();
            _writer?.WriteLine($"M_T_C_O_N_N_E_C_T_O_N{(Configuration.HeartbeatMs > 0 ? "_H" : string.Empty)}");
            _writer?.Flush();
            return true;
        }
        catch (Exception e)
        {
            Logger.Warn(e, $"[{Configuration.Name}] SHDR enable failed.");
            return false;
        }
    }
    
    private bool ShdrOff()
    {
        try
        {
            Logger.Info($"[{Configuration.Name}] SHDR disable request.");
            _writer?.WriteLine($"M_T_C_O_N_N_E_C_T_O_F_F");
            _writer?.Flush();
            return true;
        }
        catch (Exception e)
        {
            Logger.Warn(e, $"[{Configuration.Name}] SHDR disable failed.");
            return false;
        }
    }

    private bool ShdrHeartBeat()
    {
        try
        {
            if (Configuration.HeartbeatMs >= 1000 && 
                _lastHeartbeatTs + Configuration.HeartbeatMs < DateTime.UtcNow.ToEpochMilliseconds())
            {
                Logger.Info($"[{Configuration.Name}] Connection heartbeat request.");
                _writer?.WriteLine("ALIVE");
                _writer?.Flush();
                _lastHeartbeatTs = DateTime.UtcNow.ToEpochMilliseconds();
            }

            return true;
        }
        catch (Exception e)
        {
            Logger.Warn(e, $"[{Configuration.Name}] Heartbeat failed.");
            return false;
        }
    }
    
    private void Fetch()
    {
        // prevent re-entry
        if (_isReading) return;
        
        _isReading = true;

        // heartbeat, write socket, catch exceptions
        if (!ShdrHeartBeat())
        {
            Disconnect();
            MarkFaulted(new Exception($"[{Configuration.Name}] Socket write failed."));
            _isReading = false;
            return;
        }
        
        // track duration without incoming data
        if (!_stream.DataAvailable)
        {
            _durationWithoutData += Configuration.ScanIntervalMs;
        }
        else
        {
            _durationWithoutData = 0;
        }

        // request shdr enable if duration too long without data
        if (Configuration.RetryMs >= 1000 && 
            _durationWithoutData > Configuration.RetryMs)
        {
            _durationWithoutData = 0;
            if (!ShdrOn())
            {
                Disconnect();
                MarkFaulted(new Exception($"[{Configuration.Name}] Socket write failed."));
                _isReading = false;
                return;
            }
        }
        
        var data = string.Empty;

        // read data
        try
        {
            var buffer = new byte[1024];
            
            while (_stream.DataAvailable)
            {
                ++_packetCounter;
                var bytes = _stream.Read(buffer, 0, 1024);
                data += _encoding.GetString(buffer, 0, bytes);
            }
        }
        catch (Exception e)
        {
            Disconnect();
            MarkFaulted(new Exception($"[{Configuration.Name}] Socket read failed."));
            _isReading = false;
            return;
        }

        // process data
        try
        {
            if (string.IsNullOrEmpty(data))
            {
                _isReading = false;
                return;
            }
            
            // remove control characters
            data = data
                .Replace("\u0002", string.Empty) //
                .Replace("\u0003", string.Empty) //
                .Replace("\n", string.Empty);

            // split by timestamp and remove empty lines
            var rex = new Regex(@"\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}.\d{3}");
            var shdrLines = rex
                .Split(data)
                .Where(s => s != string.Empty);

            int shdrLineIdx = -1;
            foreach (var shdrLine in shdrLines)
            {
                Logger.Trace($"{_packetCounter}:{++_lineCounter}:{shdrLine}");

                ++shdrLineIdx;

                // process first line
                if (shdrLineIdx == 0)
                {
                    // line is truncated, try to recover with last line of previous packet
                    // if last line of previous packet is empty then we drop this first line
                    if (!shdrLine.StartsWith('|') && !string.IsNullOrEmpty(_lastLineFromPreviousPacket))
                    {
                        Logger.Debug($"[{Configuration.Name}] Recovering truncated line '{_lastLineFromPreviousPacket}'<=>'{shdrLine}'");
                        ProcessShdrLine($"{_lastLineFromPreviousPacket}{shdrLine}");
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(_lastLineFromPreviousPacket))
                        {
                            // process last line of previous packet
                            ProcessShdrLine(_lastLineFromPreviousPacket);
                        }

                        if (shdrLines.Count() > 1)
                        {
                            // process first line from current packet, if this is not the only line
                            ProcessShdrLine(shdrLine);
                        }
                        else
                        {
                            // save first and only line for next packet to match up with first broken shdr line
                            _lastLineFromPreviousPacket = shdrLine;
                        }
                    }
                }
                // do not process last line in packet, store it for next packet
                else if (shdrLineIdx == shdrLines.Count() - 1)
                {
                    // store line and process it next packet
                    _lastLineFromPreviousPacket = shdrLine;
                }
                // process lines m+1 to n-1
                else
                {
                    ProcessShdrLine(shdrLine);
                }
            }
        }
        catch (Exception e)
        {
            Logger.Error(e, $"[{Configuration.Name}] Processing incoming data failed.");
        }

        _isReading = false;
    }

    private void ProcessShdrLine(string shdrLine)
    {
        // TODO: handle alarm
        // alarm0|107|em stop|desc

        var shdrTokens = shdrLine.Split('|');
            //.Where(s => s != string.Empty)
            //.ToArray();
        bool wasAlarm = false;
        
        Logger.Trace($"[{Configuration.Name}] line: {shdrLine} tokens:{JsonConvert.SerializeObject(shdrTokens)}");
        
        for(var i=1; i < shdrTokens.Length; i+=2) 
        {
            if (wasAlarm)
            {
                wasAlarm = false;
                continue;
            }
            
            if (shdrTokens[i].StartsWith("alarm"))
            {
                wasAlarm = true;
            }

            //Logger.Info($"[{Configuration.Name}] i: {i}");

            lock (_incomingBufferLock)
            {
                _incomingBuffer.Add(new IncomingMessage()
                {
                    Key = shdrTokens[i],
                    Value = shdrTokens[i + 1],
                    Timestamp = DateTime.UtcNow.ToEpochMilliseconds()
                });
            }
            
        }
    }
}