namespace DIME;

public interface IConfigurationProvider
{
    public Dictionary<object, object> GetConfiguration();
}