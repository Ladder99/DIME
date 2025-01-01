using System.Text;
using Akka.Actor;
using Akka.Actor.Internal;
using DIME.Configuration.Mqtt;
using MQTTnet;

namespace DIME.Connectors.Mqtt;

public class Source: QueuingSourceConnector<ConnectorConfiguration, ConnectorItem>
{
    private MQTTnet.Client.IMqttClient _clientMqttNet = null;
    private TurboMqtt.Client.IMqttClient _clientTurboMqtt = null;

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
        if (Configuration.UseTurbo)
        {
            Logger.Warn($"[{Configuration.Name}] Using TurboMqtt");

           _clientTurboMqtt = new TurboMqtt.Client.MqttClientFactory(new ActorSystemImpl("dime"))
                .CreateTcpClient(
                    new TurboMqtt.Client.MqttClientConnectOptions(
                        Properties.GetProperty<string>("client_id"), 
                        TurboMqtt.Protocol.MqttProtocolVersion.V3_1_1), 
                    new TurboMqtt.Client.MqttClientTcpOptions(Configuration.Address, Configuration.Port))
                .GetAwaiter().GetResult();
        }
        else
        {
            _clientMqttNet = new MQTTnet.MqttFactory().CreateMqttClient();
            _clientMqttNet.DisconnectedAsync += MQTTNET_ClientOnDisconnectedAsync;
            _clientMqttNet.ApplicationMessageReceivedAsync += MQTTNET_ClientOnApplicationMessageReceivedAsync;
        }
        
        return true;
    }

    protected override bool ConnectImplementation()
    {
        if (Configuration.UseTurbo)
        {
            var result = _clientTurboMqtt.ConnectAsync().GetAwaiter().GetResult();
            
            foreach(var item in Configuration.Items.Where(x => x.Enabled && !string.IsNullOrEmpty(x.Address)))
            {
                _clientTurboMqtt.SubscribeAsync(item.Address, (TurboMqtt.QualityOfService)Configuration.QoS)
                    .GetAwaiter().GetResult();
            }

            return result.IsSuccess;
        }
        else
        {
            var clientOptions = new MQTTnet.Client.MqttClientOptionsBuilder()
                .WithClientId(Properties.GetProperty<string>("client_id"))
                .WithTcpServer(Configuration.Address, Configuration.Port)
                .WithCleanSession(Configuration.CleanSession);
            var options = clientOptions.Build();
            var result = _clientMqttNet.ConnectAsync(options).Result;
        
            var mqttSubscribeOptions = new MQTTnet.MqttFactory().CreateSubscribeOptionsBuilder();
            foreach(var item in Configuration.Items.Where(x => x.Enabled && !string.IsNullOrEmpty(x.Address)))
            {
                mqttSubscribeOptions.WithTopicFilter(f =>
                {
                    f.WithTopic(item.Address);
                    f.WithQualityOfServiceLevel((MQTTnet.Protocol.MqttQualityOfServiceLevel)Configuration.QoS);
                });
            }
            var subscribeResult = _clientMqttNet.SubscribeAsync(mqttSubscribeOptions.Build()).Result;
        
            return result.ResultCode == MQTTnet.Client.MqttClientConnectResultCode.Success;
        }
    }

    protected override bool ReadImplementation()
    {
        if (Configuration.UseTurbo)
        {
            while (_clientTurboMqtt.ReceivedMessages.TryRead(out var message))
            {
                AddToIncomingBuffer(message.Topic, Encoding.UTF8.GetString(message.Payload.Span));
            }
        }
        
        return base.ReadImplementation();
    }

    protected override bool DisconnectImplementation()
    {
        if (Configuration.UseTurbo)
        {
            _clientTurboMqtt.DisconnectAsync().GetAwaiter().GetResult();
            return true;
        }
        else
        {
            _clientMqttNet.DisconnectAsync(
                new MQTTnet.Client.MqttClientDisconnectOptions()
                {
                    Reason = MQTTnet.Client.MqttClientDisconnectOptionsReason.AdministrativeAction
                }).Wait();
            return true;
        }
    }
    
    protected override bool DeinitializeImplementation()
    {
        return true;
    }
    
    private Task MQTTNET_ClientOnApplicationMessageReceivedAsync(MQTTnet.Client.MqttApplicationMessageReceivedEventArgs arg)
    {
        AddToIncomingBuffer(arg.ApplicationMessage.Topic, arg.ApplicationMessage.ConvertPayloadToString());
        
        return Task.FromResult(0);
    }

    private Task MQTTNET_ClientOnDisconnectedAsync(MQTTnet.Client.MqttClientDisconnectedEventArgs arg)
    {
        IsConnected = false;
        return Task.FromResult(0);
    }
}