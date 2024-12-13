using System.Security.Cryptography;
using System.Text;
using DIME.Configuration.SplunkEhSdk;
using Google.Protobuf.Collections;
using Grpc.Net.Client;
using Microsoft.VisualBasic;
using Newtonsoft.Json;
using ProtoBuf.WellKnownTypes;
using Timestamp = Google.Protobuf.WellKnownTypes.Timestamp;

namespace DIME.Connectors.SplunkEhSdk;

public class Sink: SinkConnector<ConnectorConfiguration, ConnectorItem>
{
    public Sink(ConnectorConfiguration configuration, Disruptor.Dsl.Disruptor<MessageBoxMessage> disruptor) : base(configuration, disruptor)
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
        return true;
    }

    protected override bool WriteImplementation()
    {
        using (var channel = GrpcChannel.ForAddress($"{Configuration.Address}:{Configuration.Port}"))
        {
            var client = new EdgeHubService.EdgeHubServiceClient(channel);
            MD5 md5Hasher = MD5.Create();
            
            foreach (var message in Outbox)
            {
                var @event = new SendEventDataRequest()
                {
                    Id = new Guid(md5Hasher.ComputeHash(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(message)))).ToString(),
                    CreateTime = Timestamp.FromDateTime(DateTime.UtcNow),
                    Fields =
                    {
                        { "connector", message.ConnectorItemRef?.Configuration.Name },
                        { "path", message.Path },
                        { "data", JsonConvert.SerializeObject(message.Data) },
                        { "timestamp", message.Timestamp.ToString() }
                    }
                };
                var reply = client.SendEventData(@event);

                if (reply.Error is not null)
                {
                    throw new Exception(reply.Error.Message);
                }
            }
        }

        return true;
    }

    protected override bool DisconnectImplementation()
    {
        return true;
    }
    
    protected override bool DeinitializeImplementation()
    {
        return true;
    }
}