namespace DIME;

public interface IConfigurationProvider
{
    public (string, Dictionary<object, object>) ReadConfiguration();
    public (bool, string, Dictionary<object, object>) WriteConfiguration(string yamlConfiguration);
}