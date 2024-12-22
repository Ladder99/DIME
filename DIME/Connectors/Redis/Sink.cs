using DIME.Configuration.Redis;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace DIME.Connectors.Redis;

public class Sink: SinkConnector<ConnectorConfiguration, ConnectorItem>
{
    private ConnectionMultiplexer _client = null;
    private IDatabase _database = null;
    
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
        _client = ConnectionMultiplexer.Connect($"{Configuration.Address}:{Configuration.Port}");
        _database = _client.GetDatabase(Configuration.Database);
        return _client.IsConnected;
    }

    protected override bool WriteImplementation()
    {
        foreach (var message in Outbox)
        {
            _database.StringSet(message.Path, Configuration.UseSinkTransform ? TransformAndSerializeMessage(message) : JsonConvert.SerializeObject(message));
        }

        return true;
    }

    protected override bool DisconnectImplementation()
    {
        _client.Close();
        return true;
    }
    
    protected override bool DeinitializeImplementation()
    {
        return true;
    }
}