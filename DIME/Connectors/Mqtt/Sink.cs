using DIME.Configuration.Mqtt;
using MQTTnet;
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
        _client = new MqttClientFactory().CreateMqttClient();
        _client.DisconnectedAsync += ClientOnDisconnectedAsync;
        
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
        
        return true;
    }

    protected override bool WriteImplementation()
    {
        foreach (var message in Outbox)
        {
            //Console.WriteLine($"{message.Path} = {message.Data}");
            
            var msg = new MqttApplicationMessageBuilder()
                .WithTopic($"{Configuration.BaseTopic}/{message.Path}")
                .WithPayload(JsonConvert.SerializeObject(message))
                .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtMostOnce)
                .Build();

            _client.PublishAsync(msg);
        }
        
        return true;
    }

    protected override bool DisconnectImplementation()
    {
        _client.DisconnectAsync().Wait();
        return true;
    }

    private Task ClientOnDisconnectedAsync(MqttClientDisconnectedEventArgs arg)
    {
        IsConnected = false;
        return Task.FromResult(0);
    }
}