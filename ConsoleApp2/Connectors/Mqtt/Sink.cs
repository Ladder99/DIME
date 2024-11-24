using ConsoleApp2.Configuration.Mqtt;
using MQTTnet;

namespace ConsoleApp2.Connectors.Mqtt;

public class Sink: SinkConnector<ConnectorConfiguration, ConnectorItem>
{
    private IMqttClient _client = null;
    
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
        /*
        var message = new MqttApplicationMessageBuilder()
            .WithTopic($"{_configuration.GetProperty<string>("base_topic")}/{sourceConfiguration.GetProperty<string>("name")}")
            .WithPayload(JsonConvert.SerializeObject(sourceResult))
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtMostOnce)
            .Build();

        _client.PublishAsync(message);
         */
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