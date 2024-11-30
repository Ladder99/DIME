using System.Collections.Concurrent;
using IDS.Transporter.Configuration.Mqtt;
using MQTTnet;

namespace IDS.Transporter.Connectors.Mqtt;

public class Source: QueuingSourceConnector<ConnectorConfiguration, ConnectorItem>
{
    private IMqttClient _client = null;

    public Source(ConnectorConfiguration configuration, Disruptor.Dsl.Disruptor<MessageBoxMessage> disruptor) : base(configuration, disruptor)
    {
    }

    protected override bool InitializeImplementation()
    {
        Properties.SetProperty("client_id", Guid.NewGuid().ToString());
        return true;
    }

    protected override bool CreateImplementation()
    {
        _client = new MqttClientFactory().CreateMqttClient();
        _client.DisconnectedAsync += ClientOnDisconnectedAsync;
        _client.ApplicationMessageReceivedAsync += ClientOnApplicationMessageReceivedAsync;
        
        return true;
    }

    protected override bool ConnectImplementation()
    {
        var clientOptions = new MqttClientOptionsBuilder()
            .WithClientId(Properties.GetProperty<string>("client_id"))
            .WithTcpServer(Configuration.IpAddress, Configuration.Port)
            .WithCleanSession(Configuration.CleanSession);
        var options = clientOptions.Build();
        var result = _client.ConnectAsync(options).Result;
        
        IsConnected = result.ResultCode == MqttClientConnectResultCode.Success;
        
        var mqttSubscribeOptions = new MqttClientFactory().CreateSubscribeOptionsBuilder();
        foreach(var item in Configuration.Items.Where(x => x.Enabled && x.Address != null))
        {
            mqttSubscribeOptions.WithTopicFilter(f => { f.WithTopic(item.Address); });
        }
        var subscribeResult = _client.SubscribeAsync(mqttSubscribeOptions.Build()).Result;
        
        return true;
    }
    
    protected override bool DisconnectImplementation()
    {
        _client.DisconnectAsync().Wait();
        return true;
    }
    
    private Task ClientOnApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs arg)
    {
        lock (_incomingBufferLock)
        {
            _incomingBuffer.Add(new IncomingMessage()
            {
                Key = arg.ApplicationMessage.Topic,
                Value = arg.ApplicationMessage.ConvertPayloadToString(),
                Timestamp = DateTime.UtcNow.ToEpochMilliseconds()
            });
        }
        
        return Task.FromResult(0);
    }

    private Task ClientOnDisconnectedAsync(MqttClientDisconnectedEventArgs arg)
    {
        IsConnected = false;
        return Task.FromResult(0);
    }
}