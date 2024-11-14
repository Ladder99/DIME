using libplctag;
using libplctag.DataTypes;

namespace ConsoleApp2.Connectors.Source;

public class EthernetIP: ISource
{
    private PropertyBag _configuration = null;
    private List<PropertyBag> _readItems = null;
    
    public void Initialize(PropertyBag configuration, List<PropertyBag> readItems)
    {
        _configuration = configuration;
        _readItems = readItems;
        
        _configuration.MakeDefaultProperty("type", 0);
        _configuration.MakeDefaultProperty("address", "192.168.111.20");
        _configuration.MakeDefaultProperty("path", "1,0");
        _configuration.MakeDefaultProperty("log", 3);
        _configuration.MakeDefaultProperty("timeout", 1000);
        
        _configuration.SetProperty("typeEnum", _configuration.GetProperty<int>("type") switch
        {
            0 => PlcType.ControlLogix,
            1 => PlcType.Plc5,
            2 => PlcType.Slc500,
            3 => PlcType.LogixPccc,
            4 => PlcType.Micro800,
            5 => PlcType.MicroLogix,
            6 => PlcType.Omron,
            _ => PlcType.ControlLogix
        });
        
        _configuration.SetProperty("logLevel", _configuration.GetProperty<int>("log") switch
        {
            0 => DebugLevel.None,
            1 => DebugLevel.Error,
            2 => DebugLevel.Warn,
            3 => DebugLevel.Info,
            4 => DebugLevel.Detail,
            5 => DebugLevel.Spew,
            _ => DebugLevel.Error
        });
        
        LibPlcTag.DebugLevel = _configuration.GetProperty<DebugLevel>("logLevel");
    }
    
    public void Create()
    {
        
    }
    
    public void Connect()
    {
        
    }

    public List<PropertyBag> Read()
    {
        List<PropertyBag> items = new List<PropertyBag>();
        
        foreach (var readItem in _readItems)
        {
            object response = null;
            
            switch (readItem.GetProperty<string>("type").ToLower())
            {
                case "bool":
                    response = new Tag<BoolPlcMapper, bool>()
                    {
                        Name = readItem.GetProperty<string>("address"),
                        Gateway = _configuration.GetProperty<string>("address"),
                        Path = _configuration.GetProperty<string>("path"),
                        PlcType = _configuration.GetProperty<PlcType>("typeEnum"),
                        Protocol = Protocol.ab_eip,
                        Timeout = TimeSpan.FromMilliseconds(_configuration.GetProperty<int>("timeout")),
                        DebugLevel = _configuration.GetProperty<DebugLevel>("logLevel")
                    }.Read();
                    break;
                case "sint":
                    response = new Tag<SintPlcMapper, sbyte>()
                    {
                        Name = readItem.GetProperty<string>("address"),
                        Gateway = _configuration.GetProperty<string>("address"),
                        Path = _configuration.GetProperty<string>("path"),
                        PlcType = _configuration.GetProperty<PlcType>("typeEnum"),
                        Protocol = Protocol.ab_eip,
                        Timeout = TimeSpan.FromMilliseconds(_configuration.GetProperty<int>("timeout")),
                        DebugLevel = _configuration.GetProperty<DebugLevel>("logLevel")
                    }.Read();
                    break;
                case "int":
                    response = new Tag<IntPlcMapper, short>()
                    {
                        Name = readItem.GetProperty<string>("address"),
                        Gateway = _configuration.GetProperty<string>("address"),
                        Path = _configuration.GetProperty<string>("path"),
                        PlcType = _configuration.GetProperty<PlcType>("typeEnum"),
                        Protocol = Protocol.ab_eip,
                        Timeout = TimeSpan.FromMilliseconds(_configuration.GetProperty<int>("timeout")),
                        DebugLevel = _configuration.GetProperty<DebugLevel>("logLevel")
                    }.Read();
                    break;
                case "dint":
                    response = new Tag<DintPlcMapper, int>()
                    {
                        Name = readItem.GetProperty<string>("address"),
                        Gateway = _configuration.GetProperty<string>("address"),
                        Path = _configuration.GetProperty<string>("path"),
                        PlcType = _configuration.GetProperty<PlcType>("typeEnum"),
                        Protocol = Protocol.ab_eip,
                        Timeout = TimeSpan.FromMilliseconds(_configuration.GetProperty<int>("timeout")),
                        DebugLevel = _configuration.GetProperty<DebugLevel>("logLevel")
                    }.Read();
                    break;
                case "lint":
                    response = new Tag<LintPlcMapper, long>()
                    {
                        Name = readItem.GetProperty<string>("address"),
                        Gateway = _configuration.GetProperty<string>("address"),
                        Path = _configuration.GetProperty<string>("path"),
                        PlcType = _configuration.GetProperty<PlcType>("typeEnum"),
                        Protocol = Protocol.ab_eip,
                        Timeout = TimeSpan.FromMilliseconds(_configuration.GetProperty<int>("timeout")),
                        DebugLevel = _configuration.GetProperty<DebugLevel>("logLevel")
                    }.Read();
                    break;
                case "real":
                    response = new Tag<RealPlcMapper, float>()
                    {
                        Name = readItem.GetProperty<string>("address"),
                        Gateway = _configuration.GetProperty<string>("address"),
                        Path = _configuration.GetProperty<string>("path"),
                        PlcType = _configuration.GetProperty<PlcType>("typeEnum"),
                        Protocol = Protocol.ab_eip,
                        Timeout = TimeSpan.FromMilliseconds(_configuration.GetProperty<int>("timeout")),
                        DebugLevel = _configuration.GetProperty<DebugLevel>("logLevel")
                    }.Read();
                    break;
                case "string":
                    response = new Tag<StringPlcMapper, string>()
                    {
                        Name = readItem.GetProperty<string>("address"),
                        Gateway = _configuration.GetProperty<string>("address"),
                        Path = _configuration.GetProperty<string>("path"),
                        PlcType = _configuration.GetProperty<PlcType>("typeEnum"),
                        Protocol = Protocol.ab_eip,
                        Timeout = TimeSpan.FromMilliseconds(_configuration.GetProperty<int>("timeout")),
                        DebugLevel = _configuration.GetProperty<DebugLevel>("logLevel")
                    }.Read();
                    break;
                default:
                    response = null;
                    break;
            }
            
            var item = new PropertyBag();
            item.SetProperty("address", readItem.GetProperty<string>("address"));
            item.SetProperty("value", response);
            item.SetProperty("timestamp", DateTime.Now);
            items.Add(item);
        }

        return items;
    }

    public void Disconnect()
    {
        
    }
}