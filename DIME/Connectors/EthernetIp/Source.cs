using System.Net.NetworkInformation;
using DIME.Configuration.EthernetIp;
using libplctag;
using libplctag.DataTypes;
using NLog;

namespace DIME.Connectors.EthernetIp;

public class Source: PollingSourceConnector<ConnectorConfiguration, ConnectorItem>
{
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

        return true;
    }

    protected override bool CreateImplementation()
    {
        LibPlcTag.LogEvent += LibPlcTagOnLogEvent;
        return true;
    }

    protected override bool ConnectImplementation()
    {
        var pingSender = new Ping();
        string host = Configuration.IpAddress;
        int timeout = 1000;

        var reply = pingSender.Send(host, timeout);
        return reply.Status == IPStatus.Success;
    }

    protected override object ReadFromDevice(ConnectorItem item)
    {
        object response = null;
        
        switch (item.Type.ToLower())
        {
            case "bool":
                response = new Tag<BoolPlcMapper, bool>()
                {
                    Name = item.Address,
                    Gateway = Configuration.IpAddress,
                    Path = Configuration.Path,
                    PlcType = Properties.GetProperty<PlcType>("typeEnum"),
                    Protocol = Protocol.ab_eip,
                    Timeout = TimeSpan.FromMilliseconds(Configuration.TimeoutMs),
                    DebugLevel = Properties.GetProperty<DebugLevel>("logLevel")
                }.Read();
                break;
            case "sint":
                response = new Tag<SintPlcMapper, sbyte>()
                {
                    Name = item.Address,
                    Gateway = Configuration.IpAddress,
                    Path = Configuration.Path,
                    PlcType = Properties.GetProperty<PlcType>("typeEnum"),
                    Protocol = Protocol.ab_eip,
                    Timeout = TimeSpan.FromMilliseconds(Configuration.TimeoutMs),
                    DebugLevel = Properties.GetProperty<DebugLevel>("logLevel")
                }.Read();
                break;
            case "int":
                response = new Tag<IntPlcMapper, short>()
                {
                    Name = item.Address,
                    Gateway = Configuration.IpAddress,
                    Path = Configuration.Path,
                    PlcType = Properties.GetProperty<PlcType>("typeEnum"),
                    Protocol = Protocol.ab_eip,
                    Timeout = TimeSpan.FromMilliseconds(Configuration.TimeoutMs),
                    DebugLevel = Properties.GetProperty<DebugLevel>("logLevel")
                }.Read();
                break;
            case "dint":
                response = new Tag<DintPlcMapper, int>()
                {
                    Name = item.Address,
                    Gateway = Configuration.IpAddress,
                    Path = Configuration.Path,
                    PlcType = Properties.GetProperty<PlcType>("typeEnum"),
                    Protocol = Protocol.ab_eip,
                    Timeout = TimeSpan.FromMilliseconds(Configuration.TimeoutMs),
                    DebugLevel = Properties.GetProperty<DebugLevel>("logLevel")
                }.Read();
                break;
            case "lint":
                response = new Tag<LintPlcMapper, long>()
                {
                    Name = item.Address,
                    Gateway = Configuration.IpAddress,
                    Path = Configuration.Path,
                    PlcType = Properties.GetProperty<PlcType>("typeEnum"),
                    Protocol = Protocol.ab_eip,
                    Timeout = TimeSpan.FromMilliseconds(Configuration.TimeoutMs),
                    DebugLevel = Properties.GetProperty<DebugLevel>("logLevel")
                }.Read();
                break;
            case "real":
                response = new Tag<RealPlcMapper, float>()
                {
                    Name = item.Address,
                    Gateway = Configuration.IpAddress,
                    Path = Configuration.Path,
                    PlcType = Properties.GetProperty<PlcType>("typeEnum"),
                    Protocol = Protocol.ab_eip,
                    Timeout = TimeSpan.FromMilliseconds(Configuration.TimeoutMs),
                    DebugLevel = Properties.GetProperty<DebugLevel>("logLevel")
                }.Read();
                break;
            case "string":
                response = new Tag<StringPlcMapper, string>()
                {
                    Name = item.Address,
                    Gateway = Configuration.IpAddress,
                    Path = Configuration.Path,
                    PlcType = Properties.GetProperty<PlcType>("typeEnum"),
                    Protocol = Protocol.ab_eip,
                    Timeout = TimeSpan.FromMilliseconds(Configuration.TimeoutMs),
                    DebugLevel = Properties.GetProperty<DebugLevel>("logLevel")
                }.Read();
                break;
        }

        return response;
    }
    
    protected override bool DisconnectImplementation()
    {
        LibPlcTag.LogEvent -= LibPlcTagOnLogEvent;
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
        Console.WriteLine(e.Message);
        Logger.Log(logLevel, $"[libplctag] {e.Message}");
    }
}