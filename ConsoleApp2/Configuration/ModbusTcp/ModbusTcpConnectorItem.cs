namespace ConsoleApp2.Configuration.ModbusTcp;

public sealed class ConnectorItem : Configuration.ConnectorItem
{
    public int Type { get; set; }
    public ushort Address { get; set; }
    public ushort Count { get; set; }
}