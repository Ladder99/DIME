using System.Net;
using DIME.Configuration.Mqtt;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Protocol;

namespace DIME.Connectors.Mqtt;

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
        _client = new MqttFactory().CreateMqttClient();
        _client.DisconnectedAsync += ClientOnDisconnectedAsync;
        _client.ApplicationMessageReceivedAsync += ClientOnApplicationMessageReceivedAsync;
        
        return true;
    }

    protected override bool ConnectImplementation()
    {
        var clientOptions = new MqttClientOptionsBuilder()
            .WithClientId(Properties.GetProperty<string>("client_id"))
            .WithTcpServer(tcpOptions =>
            {
                tcpOptions.BufferSize = 0x100000;
            })
            .WithEndPoint(new DnsEndPoint(Configuration.Address, Configuration.Port, System.Net.Sockets.AddressFamily.InterNetwork))
            .WithCleanSession(Configuration.CleanSession);
        var options = clientOptions.Build();
        var result = _client.ConnectAsync(options).Result;
        
        var mqttSubscribeOptions = new MqttFactory().CreateSubscribeOptionsBuilder();
        foreach(var item in Configuration.Items.Where(x => x.Enabled && !string.IsNullOrEmpty(x.Address)))
        {
            mqttSubscribeOptions.WithTopicFilter(f =>
            {
                f.WithTopic(item.Address);
                f.WithQualityOfServiceLevel((MqttQualityOfServiceLevel)Configuration.QoS);
            });
        }
        _client.SubscribeAsync(mqttSubscribeOptions.Build()).Wait();
        
        return result.ResultCode == MqttClientConnectResultCode.Success;;
    }
    
    protected override bool DisconnectImplementation()
    {
        _client.DisconnectAsync().Wait();
        return true;
    }
    
    protected override bool DeinitializeImplementation()
    {
        _client.DisconnectedAsync -= ClientOnDisconnectedAsync;
        _client.ApplicationMessageReceivedAsync -= ClientOnApplicationMessageReceivedAsync;
        return true;
    }
    
    private Task ClientOnApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs arg)
    {
        AddToIncomingBuffer(arg.ApplicationMessage.Topic, arg.ApplicationMessage.ConvertPayloadToString());
        return Task.FromResult(0);
    }

    private Task ClientOnDisconnectedAsync(MqttClientDisconnectedEventArgs arg)
    {
        IsConnected = false;
        return Task.FromResult(0);
    }
}