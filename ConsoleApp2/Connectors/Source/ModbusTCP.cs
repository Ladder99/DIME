using System.Net.Sockets;
using NModbus;

namespace ConsoleApp2.Connectors.Source;

public class ModbusTCP: ISource
{
    private PropertyBag _configuration = null;
    private List<PropertyBag> _readItems = null;
    
    public PropertyBag ConnectorConfiguration { get { return _configuration; } }
    public List<PropertyBag> ItemsConfiguration { get { return _readItems; } }

    private IModbusMaster _client = null;
    
    public void Initialize(PropertyBag configuration, List<PropertyBag> readItems)
    {
        _configuration = configuration;
        _readItems = readItems;
        
        _configuration.MakeDefaultProperty("enabled", true);
        _configuration.MakeDefaultProperty("address", "192.168.1.1");
        _configuration.MakeDefaultProperty("port", 502);
        _configuration.MakeDefaultProperty("slave", 1);
        _configuration.MakeDefaultProperty("timeout", 1000);
    }
    
    public void Create()
    {
        if (!_configuration.GetProperty<bool>("enabled"))
        {
            return;
        }
    }
    
    public void Connect()
    {
        if (!_configuration.GetProperty<bool>("enabled"))
        {
            return;
        }
        
        var tcpClient = new TcpClient();
        var task = tcpClient.ConnectAsync(
            _configuration.GetProperty<string>("address"),
            _configuration.GetProperty<int>("port")
        );
        task.Wait(_configuration.GetProperty<int>("timeout"));
        //if (!tcpClient.Connected)
        //{
        //    return false;
        //}
        _client = new ModbusFactory().CreateMaster(tcpClient);
        _client.Transport.ReadTimeout = _configuration.GetProperty<int>("timeout");
        _client.Transport.WriteTimeout = _configuration.GetProperty<int>("timeout");
    }

    public List<PropertyBag> Read()
    {
        if (!_configuration.GetProperty<bool>("enabled"))
        {
            return new List<PropertyBag>();
        }
        
        List<PropertyBag> items = new List<PropertyBag>();
        
        foreach (var readItem in _readItems)
        {
            object response = null;
            
            switch (readItem.GetProperty<int>("type"))
            {
                case 1:
                    response = _client
                        .ReadCoils(_configuration.GetProperty<byte>("slave"), 
                            readItem.GetProperty<ushort>("address"), 
                            readItem.GetProperty<ushort>("count"));
                    break;
                case 2:
                    response = _client
                        .ReadInputs(_configuration.GetProperty<byte>("slave"), 
                            readItem.GetProperty<ushort>("address"), 
                            readItem.GetProperty<ushort>("count"));
                    break;
                case 3:
                    response = _client
                        .ReadHoldingRegisters(_configuration.GetProperty<byte>("slave"), 
                            readItem.GetProperty<ushort>("address"), 
                            readItem.GetProperty<ushort>("count"));
                    break;
                case 4:
                    response = _client
                        .ReadInputRegisters(_configuration.GetProperty<byte>("slave"), 
                            readItem.GetProperty<ushort>("address"), 
                            readItem.GetProperty<ushort>("count"));
                    break;
                default:
                    response = null;
                    break;
            }
            
            var item = new PropertyBag();
            //TODO: prepend register type to address (e.g.  type=1 address=12 => 10012)
            item.SetProperty("address", readItem.GetProperty<string>("address"));
            item.SetProperty("value", response);
            item.SetProperty("timestamp", DateTime.Now);
            items.Add(item);
        }

        return items;
    }

    public void Disconnect()
    {
        if (!_configuration.GetProperty<bool>("enabled"))
        {
            return;
        }
    }
}