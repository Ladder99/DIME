using IDS.Transporter.Configuration.Mqtt;
using MQTTnet;
using MQTTnet.Protocol;
using Newtonsoft.Json;

namespace IDS.Transporter.Connectors.Mqtt;

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
        foreach (var response in Outbox)
        {
            Console.WriteLine($"{response.Path} = {response.Data}");
            
            var message = new MqttApplicationMessageBuilder()
                .WithTopic($"{Configuration.BaseTopic}/{response.Path}")
                .WithPayload(JsonConvert.SerializeObject(response))
                .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtMostOnce)
                .Build();

            _client.PublishAsync(message);
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