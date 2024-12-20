using System.Reflection;
using DIME.Configuration;
using DIME.Connectors;
using NLua;
using YamlDotNet.Serialization;

namespace DIME;

public class LuaRunner
{
    protected NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
    
    private NLua.Lua _state;
    private ISourceConnector _connector;
    
    public bool Initialize(ISourceConnector connector)
    {
        _connector = connector;

        try
        {
            _state = new NLua.Lua();
            _state.LoadCLRPackage();
            _state.DoString("package.path = package.path .. ';./Lua/?.lua'");
            _state["result"] = null;
            _state["this"] = null;
            _state.RegisterFunction("cache_ts", this, GetType().GetMethod("GetPrimaryCacheWithTimestamp", BindingFlags.NonPublic | BindingFlags.Instance));
            _state.RegisterFunction("cache", this, GetType().GetMethod("GetPrimaryCache", BindingFlags.NonPublic | BindingFlags.Instance));
            _state.RegisterFunction("set", this, GetType().GetMethod("SetUserCache", BindingFlags.NonPublic | BindingFlags.Instance));
            _state.RegisterFunction("configuration", this, GetType().GetMethod("GetConnectorConfiguration", BindingFlags.NonPublic | BindingFlags.Instance));
            _state.RegisterFunction("connector", this, GetType().GetMethod("GetConnector", BindingFlags.NonPublic | BindingFlags.Instance));
            _state.RegisterFunction("emit", this, GetType().GetMethod("EmitSample", BindingFlags.NonPublic | BindingFlags.Instance));
            return true;
        }
        catch (Exception e)
        {
            Logger.Error(e, "Lua runner failed to initialize.");
            return false;
        }
    }
    
    public object this[string key]
    {
        set => _state[key] = value;
    }

    public void SetContext(object result, object @this)
    {
        _state["result"] = result;
        _state["this"] = @this;
    }

    public object[] DoString(string chunk)
    {
        try
        {
            return MakeDotNetFriendly(_state.DoString(chunk));
        }
        catch (Exception e)
        {
            Logger.Error(e, $"[{_connector.Configuration.Name}] Chunk execution failed: {chunk}");
            throw;
        }
    }
    
    public object[] DoString(ConnectorItem item)
    {
        try
        {
            return MakeDotNetFriendly(_state.DoString(item.Script));
        }
        catch (Exception e)
        {
            Logger.Error(e, $"[{_connector.Configuration.Name}/{item.Name}] Chunk execution failed: {item.Script}");
            throw;
        }
    }
    
    private object[] MakeDotNetFriendly(object[] objects)
    {
        for (int i = 0; i < objects.Length; i++)
        {
            var obj = objects[i];

            if (obj is not null)
            {
                if (obj.GetType() == typeof(LuaTable))
                {
                    LuaTable table = (LuaTable)obj;
                    Dictionary<object, object> dict = new Dictionary<object, object>();

                    foreach (KeyValuePair<object, object> row in table)
                    {
                        dict[row.Key] = row.Value;
                    }

                    objects[i] = dict;
                }
            }
        }
        
        return objects;
    }

    private (string, string) MakeCachePath(string path)
    {
        var pathSlugs = path.Split('/');

        if (pathSlugs.Length == 1)
        {
            Array.Resize(ref pathSlugs, 2);
            pathSlugs[1] = pathSlugs[0];
            pathSlugs[0] = ".";
        }
        
        if (pathSlugs[0] == ".")
        {
            pathSlugs[0] = _connector.Configuration.Name;
            path = string.Join("/", pathSlugs);
        }

        return (pathSlugs[0], path);
    }
    
    private MessageBoxMessage GetPrimaryCacheMessage(string path, bool skipTagValue)
    {
        MessageBoxMessage value = null;
        
        var (connectorName, fullPath) = MakeCachePath(path);
        
        try
        {
            var runner = _connector.Runner.Runners
                .First(x => x.Connector.Configuration.Name == connectorName);
            
            var connector = runner.Connector as ISourceConnector;
            
            try
            {
                value = connector.Samples.Last(x => x.Path == fullPath);
            }
            catch (InvalidOperationException e1)
            {
                if (!connector.Current.TryGetValue(fullPath, out value))
                {
                    if (!connector.UserCache.TryGetValue(fullPath, out value))
                    {
                        if (!skipTagValue)
                        {
                            connector.TagValues.TryGetValue(fullPath, out value);
                        }
                    }
                }
            }
        }
        catch (InvalidOperationException e)
        {
            
        }
        
        return value;
    }
    
    private (object?, object?) GetPrimaryCacheWithTimestamp(string path, object? defaultValue = null, bool skipTagValue = true)
    {
        var message = GetPrimaryCacheMessage(path, skipTagValue);
        return message is null ? (defaultValue, 0) : (message.Data, message.Timestamp);
    }

    private object? GetPrimaryCache(string path, object? defaultValue = null, bool skipTagValue = true)
    {
        var message = GetPrimaryCacheMessage(path, skipTagValue);
        return message is null ? defaultValue : message.Data;
    }

    private object? SetUserCache(string key, object? value = null)
    {
        key = key.Split('/').Last();
        var path = $"{_connector.Configuration.Name}/{key}";
        
        _connector.UserCache[path] = new MessageBoxMessage()
        {
            Path = path,
            Data = value,
            Timestamp = DateTime.Now.ToEpochMilliseconds()
        };
        return value;
    }
     
    private object? GetConnectorConfiguration()
    {
        return _connector.Configuration;
    }
    
    private object? GetConnector()
    {
        return _connector;
    }

    private object? EmitSample(string path, object? value)
    {
        var item = _state["this"] as ConnectorItem;
        if (item is null) return null;
        
        var (connectorName, fullPath) = MakeCachePath(path);
        
        _connector.Samples.Add(new MessageBoxMessage()
        {
            Path = $"{fullPath}",
            Data = value,
            Timestamp = DateTime.UtcNow.ToEpochMilliseconds(),
            ConnectorItemRef = item
        });

        return value;
    }
}