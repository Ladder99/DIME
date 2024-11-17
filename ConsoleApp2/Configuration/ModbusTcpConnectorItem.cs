namespace ConsoleApp2.Configuration;

public sealed class ModbusTcpConnectorItem : ConnectorItem
{
    public int Type { get; set; }
    public ushort Address { get; set; }
    public ushort Count { get; set; }
}