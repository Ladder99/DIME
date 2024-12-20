using System.CodeDom;
using DIME.Configuration.Mqtt;
using IronPython.Modules;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Protocol;
using Newtonsoft.Json;

namespace DIME.Connectors.Mqtt;

public class Sink: SinkConnector<ConnectorConfiguration, ConnectorItem>
{
    private IMqttClient _client = null;

    public Sink(ConnectorConfiguration configuration, Disruptor.Dsl.Disruptor<MessageBoxMessage> disruptor) : base(configuration, disruptor)
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
        
        return true;
    }

    protected override bool ConnectImplementation()
    {
        var clientOptions = new MqttClientOptionsBuilder()
            .WithClientId(Properties.GetProperty<string>("client_id"))
            .WithTcpServer(Configuration.Address, Configuration.Port)
            .WithCleanSession(Configuration.CleanSession);
        var options = clientOptions.Build();
        var result = _client.ConnectAsync(options).Result;
        
        return result.ResultCode == MqttClientConnectResultCode.Success;
    }

    protected override bool WriteImplementation()
    {
        foreach (var message in Outbox)
        {
            var msgBuilder = new MqttApplicationMessageBuilder()
                .WithTopic($"{Configuration.BaseTopic}/{message.Path}")
                .WithPayload(TransformAndSerializeMessage(message))
                .WithQualityOfServiceLevel((MqttQualityOfServiceLevel)Configuration.QoS);

            if (Configuration.RetainPublish)
            {
                msgBuilder.WithRetainFlag();
            }
            
            var msg = msgBuilder.Build();

            _client.PublishAsync(msg);
        }
        
        return true;
    }

    protected override bool DisconnectImplementation()
    {
        _client.DisconnectAsync().Wait();
        return true;
    }
    
    protected override bool DeinitializeImplementation()
    {
        return true;
    }

    private Task ClientOnDisconnectedAsync(MqttClientDisconnectedEventArgs arg)
    {
        IsConnected = false;
        return Task.FromResult(0);
    }
}