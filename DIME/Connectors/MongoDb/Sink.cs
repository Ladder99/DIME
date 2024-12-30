using DIME.Configuration.MongoDb;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json;

namespace DIME.Connectors.MongoDb;

public class Sink: SinkConnector<ConnectorConfiguration, ConnectorItem>
{
    private MongoClient _client = null;
    private IMongoDatabase _database = null;
    private IMongoCollection<BsonDocument> _collection = null;
    
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
        _client = new MongoClient(Configuration.Address);
        _database = _client.GetDatabase(Configuration.Database);
        _collection = _database.GetCollection<BsonDocument>(Configuration.Collection);
        return true;
    }

    protected override bool WriteImplementation()
    {
        foreach (var message in Outbox)
        {
            var data = Configuration.UseSinkTransform ? TransformAndSerializeMessageToObject(message) : message.Data;
            
            _collection.InsertOne(new BsonDocument
            {
                { "Path", message.Path },
                { "Data", BsonValue.Create(data) },
                { "Timestamp", DateTimeOffset.FromUnixTimeMilliseconds(message.Timestamp).UtcDateTime }
            });
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