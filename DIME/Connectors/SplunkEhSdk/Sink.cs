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
                if (message.Data is null)
                {
                    continue;
                }
                
                var id = new Guid(md5Hasher.ComputeHash(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(message)))).ToString();

                float data = 0f;
                var isNumeric = IsNumericDatatype(message.Data);
                var isString = IsStringDatatype(message.Data);
                var sendAsEvent = !Configuration.NumbersToMetrics;

                if (Configuration.NumbersToMetrics)
                {
                    if (isNumeric)
                    {
                        data = Convert.ToSingle(message.Data);
                    }
                    else if (isString)
                    {
                        if (!Single.TryParse(message.Data.ToString(), out data))
                        {
                            sendAsEvent = true;
                        }
                    }
                }
                
                if (!sendAsEvent)
                {
                    var metric = new SendMetricDataRequest()
                    {
                        Id = id,
                        CreateTime = Timestamp.FromDateTime(DateTime.UtcNow),
                        Metrics =
                        {
                            new Metric()
                            {
                                Name= $"{message.ConnectorItemRef?.Configuration.Name}/{message.Path}",
                                Value = data
                            }
                        },
                        Dimensions =
                        {
                            { "connector", message.ConnectorItemRef?.Configuration.Name },
                            { "path", message.Path }
                        }
                    };
                    var reply = client.SendMetricData(metric);
                            
                    if (reply.Error is not null)
                    {
                        throw new Exception(reply.Error.Message);
                    }
                }
                else
                {
                    var @event = new SendEventDataRequest()
                    {
                        Id = id,
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
    
    private bool IsNumericDatatype(object obj) {
        switch (Type.GetTypeCode(obj.GetType())) {
            case TypeCode.Byte:
            case TypeCode.SByte:
            case TypeCode.UInt16:
            case TypeCode.UInt32:
            case TypeCode.UInt64:
            case TypeCode.Int16:
            case TypeCode.Int32:
            case TypeCode.Int64:
            case TypeCode.Decimal:
            case TypeCode.Double:
            case TypeCode.Single:
                return true;
            default:
                return false;
        }
    }
    
    private bool IsStringDatatype(object obj) {
        switch (Type.GetTypeCode(obj.GetType())) {
            case TypeCode.String:
            case TypeCode.Char:
                return true;
            default:
                return false;
        }
    }
}