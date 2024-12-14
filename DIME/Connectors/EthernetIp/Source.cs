using System.Net.NetworkInformation;
using DIME.Configuration.EthernetIp;
using libplctag;
using libplctag.DataTypes;
using Microsoft.Extensions.FileSystemGlobbing.Internal.PatternContexts;
using NLog;

namespace DIME.Connectors.EthernetIp;

public class Source: PollingSourceConnector<ConnectorConfiguration, ConnectorItem>
{
    private object _lock = new();
    
    public Source(ConnectorConfiguration configuration, Disruptor.Dsl.Disruptor<MessageBoxMessage> disruptor) : base(configuration, disruptor)
    {
    }
    
    protected override bool InitializeImplementation()
    {
        Properties.SetProperty("typeEnum", Configuration.PlcType switch
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
        
        Properties.SetProperty("logLevel", Configuration.Log switch
        {
            0 => DebugLevel.None,
            1 => DebugLevel.Error,
            2 => DebugLevel.Warn,
            3 => DebugLevel.Info,
            4 => DebugLevel.Detail,
            5 => DebugLevel.Spew,
            _ => DebugLevel.Error
        });
            
        LibPlcTag.DebugLevel = Properties.GetProperty<DebugLevel>("logLevel");
        LibPlcTag.LogEvent += LibPlcTagOnLogEvent;

        return true;
    }

    protected override bool CreateImplementation()
    {
        return true;
    }

    protected override bool ConnectImplementation()
    {
        return Configuration.BypassPing 
            ? true 
            : new Ping().Send(Configuration.Address, 1000).Status == IPStatus.Success;
    }

    protected override object ReadFromDevice(ConnectorItem item)
    {
        object response = null;
        
        lock (_lock)
        {
            switch (item.Type.ToLower())
            {
                case "bool":
                    var tag1 = new Tag<BoolPlcMapper, bool>()
                    {
                        Name = item.Address,
                        Gateway = Configuration.Address,
                        Path = Configuration.Path,
                        PlcType = Properties.GetProperty<PlcType>("typeEnum"),
                        Protocol = Protocol.ab_eip,
                        Timeout = TimeSpan.FromMilliseconds(Configuration.TimeoutMs),
                        DebugLevel = Properties.GetProperty<DebugLevel>("logLevel")
                    };
                    response = tag1.Read();
                    tag1.Dispose();
                    break;
                case "sint":
                    var tag2 = new Tag<SintPlcMapper, sbyte>()
                    {
                        Name = item.Address,
                        Gateway = Configuration.Address,
                        Path = Configuration.Path,
                        PlcType = Properties.GetProperty<PlcType>("typeEnum"),
                        Protocol = Protocol.ab_eip,
                        Timeout = TimeSpan.FromMilliseconds(Configuration.TimeoutMs),
                        DebugLevel = Properties.GetProperty<DebugLevel>("logLevel")
                    };
                    response = tag2.Read();
                    tag2.Dispose();
                    break;
                case "int":
                    var tag3 = new Tag<IntPlcMapper, short>()
                    {
                        Name = item.Address,
                        Gateway = Configuration.Address,
                        Path = Configuration.Path,
                        PlcType = Properties.GetProperty<PlcType>("typeEnum"),
                        Protocol = Protocol.ab_eip,
                        Timeout = TimeSpan.FromMilliseconds(Configuration.TimeoutMs),
                        DebugLevel = Properties.GetProperty<DebugLevel>("logLevel")
                    };
                    response = tag3.Read();
                    tag3.Dispose();
                    break;
                case "dint":
                    var tag4 = new Tag<DintPlcMapper, int>()
                    {
                        Name = item.Address,
                        Gateway = Configuration.Address,
                        Path = Configuration.Path,
                        PlcType = Properties.GetProperty<PlcType>("typeEnum"),
                        Protocol = Protocol.ab_eip,
                        Timeout = TimeSpan.FromMilliseconds(Configuration.TimeoutMs),
                        DebugLevel = Properties.GetProperty<DebugLevel>("logLevel")
                    };
                    response = tag4.Read();
                    tag4.Dispose();
                    break;
                case "lint":
                    var tag5 = new Tag<LintPlcMapper, long>()
                    {
                        Name = item.Address,
                        Gateway = Configuration.Address,
                        Path = Configuration.Path,
                        PlcType = Properties.GetProperty<PlcType>("typeEnum"),
                        Protocol = Protocol.ab_eip,
                        Timeout = TimeSpan.FromMilliseconds(Configuration.TimeoutMs),
                        DebugLevel = Properties.GetProperty<DebugLevel>("logLevel")
                    };
                    response = tag5.Read();
                    tag5.Dispose();
                    break;
                case "real":
                    var tag6 = new Tag<RealPlcMapper, float>()
                    {
                        Name = item.Address,
                        Gateway = Configuration.Address,
                        Path = Configuration.Path,
                        PlcType = Properties.GetProperty<PlcType>("typeEnum"),
                        Protocol = Protocol.ab_eip,
                        Timeout = TimeSpan.FromMilliseconds(Configuration.TimeoutMs),
                        DebugLevel = Properties.GetProperty<DebugLevel>("logLevel")
                    };
                    response = tag6.Read();
                    tag6.Dispose();
                    break;
                case "string":
                    var tag7 = new Tag<StringPlcMapper, string>()
                    {
                        Name = item.Address,
                        Gateway = Configuration.Address,
                        Path = Configuration.Path,
                        PlcType = Properties.GetProperty<PlcType>("typeEnum"),
                        Protocol = Protocol.ab_eip,
                        Timeout = TimeSpan.FromMilliseconds(Configuration.TimeoutMs),
                        DebugLevel = Properties.GetProperty<DebugLevel>("logLevel")
                    };
                    response = tag7.Read();
                    tag7.Dispose();
                    break;
            }
        }

        return response;
    }
    
    protected override bool DisconnectImplementation()
    {
        return true;
    }
    
    protected override bool DeinitializeImplementation()
    {
        LibPlcTag.LogEvent -= LibPlcTagOnLogEvent;
        lock (_lock)
        {
            LibPlcTag.Shutdown();
        }
        return true;
    }
    
    private void LibPlcTagOnLogEvent(object? sender, LogEventArgs e)
    {
        var logLevel = e.DebugLevel switch
        {
            DebugLevel.None     => LogLevel.Off,
            DebugLevel.Error    => LogLevel.Error,
            DebugLevel.Warn     => LogLevel.Warn,
            DebugLevel.Info     => LogLevel.Info,
            DebugLevel.Detail   => LogLevel.Debug,
            DebugLevel.Spew     => LogLevel.Trace,
            _                   => LogLevel.Warn,
        };
        
        Logger.Log(logLevel, $"[libplctag] {e.Message}");
    }
}