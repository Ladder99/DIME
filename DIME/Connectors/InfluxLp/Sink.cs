using DIME.Configuration.InfluxLp;
using InfluxDB3.Client;
using InfluxDB3.Client.Write;

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
        _client = new InfluxDBClient(Configuration.Address, Configuration.Token);
        return true;
    }

    protected override bool ConnectImplementation()
    {
        return true;
    }

    protected override bool WriteImplementation()
    {
        foreach (var message in Outbox)
        {
            var point = PointData.Measurement(message.Path)
                .SetField("value", Configuration.UseSinkTransform ? TransformAndSerializeMessage(message) : message.Data)
                .SetTimestamp(message.Timestamp, WritePrecision.Ms);

            _client.WritePointAsync(point, Configuration.BucketName).GetAwaiter().GetResult();
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