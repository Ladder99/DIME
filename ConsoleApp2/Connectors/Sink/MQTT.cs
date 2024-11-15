using System.Text;
using MQTTnet;
using MQTTnet.Protocol;
using Newtonsoft.Json;

namespace ConsoleApp2.Connectors.Sink;

public class MQTT: ISink
{
    private PropertyBag _configuration = null;
    
    private IMqttClient _client = null;
    
    public void Initialize(PropertyBag configuration)
    {
        _configuration = configuration;
        
        _configuration.MakeDefaultProperty("enabled", true);
        _configuration.MakeDefaultProperty("address", "127.0.0.1");
        _configuration.MakeDefaultProperty("port", 1883);
        _configuration.MakeDefaultProperty("username", string.Empty);
        _configuration.MakeDefaultProperty("password", string.Empty);
        _configuration.MakeDefaultProperty("base_topic", "ids");
        
        _configuration.MakeDefaultProperty("client_id", Guid.NewGuid().ToString());
        _configuration.MakeDefaultProperty("clean_session", true);
        
    }

    public void Create()
    {
        if (!_configuration.GetProperty<bool>("enabled"))
        {
            return;
        }
        
        _client = new MqttClientFactory().CreateMqttClient();
        _client.DisconnectedAsync += ClientOnDisconnectedAsync;
    }

    public void Connect()
    {
        if (!_configuration.GetProperty<bool>("enabled"))
        {
            return;
        }
        
        var clientOptions = new MqttClientOptionsBuilder()
            .WithClientId(_configuration.GetProperty<string>("client_id"))
            .WithTcpServer(_configuration.GetProperty<string>("address"), _configuration.GetProperty<int>("port"))
            .WithCleanSession(_configuration.GetProperty<bool>("clean_session"));
        var options = clientOptions.Build();
        var result = _client.ConnectAsync(options).Result;
        //if (result.ResultCode != MqttClientConnectResultCode.Success)
        //    return false;
    }

    public void Disconnect()
    {
        if (!_configuration.GetProperty<bool>("enabled"))
        {
            return;
        }
        
        _client.DisconnectAsync().Wait();
    }

    public void Write(PropertyBag sourceConfiguration, List<PropertyBag> sourceItems, List<PropertyBag> sourceResults)
    {
        foreach (var sourceResult in sourceResults)
        {
            var message = new MqttApplicationMessageBuilder()
                .WithTopic($"{_configuration.GetProperty<string>("base_topic")}/{sourceConfiguration.GetProperty<string>("name")}")
                .WithPayload(JsonConvert.SerializeObject(sourceResult))
                .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtMostOnce)
                .Build();
            
            _client.PublishAsync(message);
        }
        
        
    }


    private Task ClientOnDisconnectedAsync(MqttClientDisconnectedEventArgs arg)
    {
        return Task.FromResult(0);
    }
}