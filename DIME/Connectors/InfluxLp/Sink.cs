using DIME.Configuration.InfluxLp;
using InfluxDB.Client;
using InfluxDB.Client.Api.Domain;
using InfluxDB.Client.Writes;

namespace DIME.Connectors.InfluxLp;

public class Sink: SinkConnector<ConnectorConfiguration, ConnectorItem>
{
    private InfluxDBClient _client = null;
    public Sink(ConnectorConfiguration configuration, Disruptor.Dsl.Disruptor<MessageBoxMessage> disruptor) : base(configuration, disruptor)
    {
    }

    protected override bool InitializeImplementation()
    {
        return true;
    }

    protected override bool CreateImplementation()
    {
        _client = new InfluxDBClient($"http://{Configuration.Address}:{Configuration.Port}", Configuration.Token);
        return true;
    }

    protected override bool ConnectImplementation()
    {
        return _client.PingAsync().GetAwaiter().GetResult();
    }

    protected override bool WriteImplementation()
    {
        using var writeApi = _client.GetWriteApi();
        
        foreach (var message in Outbox)
        {
            var point = PointData.Measurement(message.Path)
                .Field("value", Configuration.UseSinkTransform ? TransformAndSerializeMessage(message) : message.Data)
                .Timestamp(message.Timestamp, WritePrecision.Ms);

            writeApi.WritePoint(point, Configuration.BucketName, Configuration.OrgId);
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