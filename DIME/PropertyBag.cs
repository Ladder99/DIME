namespace DIME;

public class PropertyBag
{
    private readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
    public Dictionary<string,object> Properties { get; } = new Dictionary<string, object>();

    public PropertyBag()
    {
    }
    
    public T GetProperty<T>(string key, T defaultValue = default(T))
    {
        if (Properties.ContainsKey(key))
        {
            if (Properties[key] is T) {
                return (T)Properties[key];
            } 
            try {
                return (T)Convert.ChangeType(Properties[key], typeof(T));
            } 
            catch (InvalidCastException) {
                Logger.Warn($"Unable to cast '{key}' property.");
                return default(T);
            }
        }
        else
        {
            return defaultValue;
        }
    }
    
    public void SetProperty(string key, object value)
    {
        Properties[key] = value;
    }

    public void MakeDefaultProperty(string key, object defaultValue)
    {
        Properties.TryAdd(key, defaultValue);
    }
}