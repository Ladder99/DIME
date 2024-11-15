namespace ConsoleApp2.Connectors;

public interface ISink
{
    public void Initialize(PropertyBag configuration);
    public void Create();
    public void Connect();
    public void Disconnect();
    public void Write(PropertyBag sourceConfiguration, List<PropertyBag> sourceItems, List<PropertyBag> sourceResults);
}