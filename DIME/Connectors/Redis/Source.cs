using DIME.Configuration.Redis;
using StackExchange.Redis;

namespace DIME.Connectors.Redis;

public class Source: QueuingSourceConnector<ConnectorConfiguration, ConnectorItem>
{
    private ConnectionMultiplexer _client = null;
    private IServer _server = null;
    private IDatabase _database = null;
    private ISubscriber _subscriber = null;
    private Dictionary<string, string> _messages;
    
    public Source(ConnectorConfiguration configuration, Disruptor.Dsl.Disruptor<MessageBoxMessage> disruptor) : base(configuration, disruptor)
    {
    }

    protected override bool InitializeImplementation()
    {
        _messages = new Dictionary<string, string>();
        return true;
    }

    protected override bool CreateImplementation()
    {
        return true;
    }

    protected override bool ConnectImplementation()
    {
        _client = ConnectionMultiplexer.Connect($"{Configuration.Address}:{Configuration.Port}");
        _server = _client.GetServer($"{Configuration.Address}:{Configuration.Port}");
        _database = _client.GetDatabase(Configuration.Database);
        _subscriber = _client.GetSubscriber();
        var keys =  _server.Keys(Configuration.Database).ToList();

        foreach (var key in keys)
        {
            AddToIncomingBuffer(key, _database.StringGet(key));
        }

        _subscriber.Subscribe($"__keyevent@{Configuration.Database}__:*", OnKeyspaceChanged);
        
        return _client.IsConnected;
    }

    private void OnKeyspaceChanged(RedisChannel channel, RedisValue key)
    {
        var type = channel.ToString().Split(':')[1];

        switch (type)
        {
            case "set":
                if (key.ToString() == "eipSource1/Execution")
                {
                    int i = 0;
                }

                var value = _database.StringGet(key.ToString());
                AddToIncomingBuffer(key, value);
                break;
            default:
                break;
        }
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