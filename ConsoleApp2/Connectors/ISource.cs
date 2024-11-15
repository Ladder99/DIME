namespace ConsoleApp2.Connectors;

public interface ISource
{
    public PropertyBag ConnectorConfiguration { get; }
    public List<PropertyBag> ItemsConfiguration { get; }
    public void Initialize(PropertyBag configuration, List<PropertyBag> readItems);
    public void Create();
    public void Connect();
    public void Disconnect();
    public List<PropertyBag> Read();
}