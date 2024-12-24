using DIME.Configuration.OpcDa;
using TitaniumAS.Opc.Client.Common;
using TitaniumAS.Opc.Client.Da;

namespace DIME.Connectors.OpcDa;

public class Source: BatchPollingSourceConnector<ConnectorConfiguration, ConnectorItem>
{
    private OpcDaServer _client = null;
    private OpcDaGroup _group = null;
    private OpcDaItemValue[] _values = null;

    public Source(ConnectorConfiguration configuration, Disruptor.Dsl.Disruptor<MessageBoxMessage> disruptor) : base(configuration, disruptor)
    {
    }

    protected override bool InitializeImplementation()
    { 
        return true;
    }
    
    protected override bool CreateImplementation()
    {
        Uri uri = UrlBuilder.Build(Configuration.Address);
        _client = new OpcDaServer(uri);
        return true;
    }

    protected override bool ConnectImplementation()
    {
        _client.Connect();

        _group = _client.AddGroup("DimeGroup");
        _group.IsActive = true;
        
        var definitions = new List<OpcDaItemDefinition>();

        foreach (var item in Configuration.Items.Where(x => x.Enabled))
        {
            var definition = new OpcDaItemDefinition
            {
                ItemId = item.Address,
                IsActive = true
            };
            
            definitions.Add(definition);
        }
        
        var results = _group.AddItems(definitions);
        
        foreach (var result in results)
        {
            if (result.Error.Failed)
                System.Console.WriteLine("Error adding items: {0}", result.Error);
        }
        
        return _client.IsConnected;
    }
    
    protected override bool ReadBatchFromDevice()
    {
        _values = _group.Read(_group.Items, OpcDaDataSource.Device);
        return true;
    }

    protected override object ReadItemFromBatch(Configuration.ConnectorItem item)
    {
        try
        {
            return _values.First(x => x.Item.ItemId == item.Address).Value;
        }
        catch (InvalidOperationException e)
        {
            return null;
        }
    }
    
    protected override bool DisconnectImplementation()
    {
        _client.RemoveGroup(_group);
        _client.Disconnect();
        return !_client.IsConnected;
    }
    
    protected override bool DeinitializeImplementation()
    {
        return true;
    }
}