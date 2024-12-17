namespace DIME.Connectors.MtConnectShdr.DeviceBuilder;

public class PathPart
{
    public string Name { get; set; }
    public Dictionary<string, string> Attributes { get; set; } = new();
}