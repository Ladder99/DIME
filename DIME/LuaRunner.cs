using System.Reflection;
using DIME.Configuration;
using DIME.Connectors;

namespace DIME;

public class LuaRunner
{
    protected NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
    
    private NLua.Lua _state;
    private ISourceConnector _connector;
    private Dictionary<string, object> _userCache = new ();
    
    public bool Initialize(ISourceConnector connector)
    {
        _connector = connector;
        
        try
        {
            _state = new NLua.Lua();
            _state.LoadCLRPackage();
            _state["result"] = null;
            _state.RegisterFunction("cache", this, GetType().GetMethod("GetPrimaryCache", BindingFlags.NonPublic | BindingFlags.Instance));
            _state.RegisterFunction("get", this, GetType().GetMethod("GetUserCache", BindingFlags.NonPublic | BindingFlags.Instance));
            _state.RegisterFunction("set", this, GetType().GetMethod("SetUserCache", BindingFlags.NonPublic | BindingFlags.Instance));
            var initResult = _state.DoString(_connector.Configuration.InitScript ?? "");
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

    public object[] DoString(ConnectorItem item)
    {
        try
        {
            return _state.DoString(item.Script);
        }
        catch (Exception e)
        {
            Logger.Error($"[{_connector.Configuration.Name}/{item.Name}] Chunk execution failed: {item.Script}");
            throw;
        }
    }
    
    private object? GetPrimaryCache(string path, object? defaultValue = null)
    {
        object? value = null;
        
        var pathSlugs = path.Split('/');
        if (pathSlugs[0] == ".")
        {
            pathSlugs[0] = _connector.Configuration.Name;
            path = string.Join("/", pathSlugs);
        }
        try
        {
            var runner = _connector.Runner.Runners
                .First(x => x.Connector.Configuration.Name == pathSlugs[0]);
            
            var connector = runner.Connector as ISourceConnector;
            
            try
            {
                var message = connector?.Samples.Last(x => x.Path == path);
                value = message?.Data;
            }
            catch (InvalidOperationException e1)
            {
                try
                {
                    var message = connector?.Current.Last(x => x.Path == path);
                    value = message?.Data;
                }
                catch (InvalidOperationException e2)
                {
                
                }
            }
        }
        catch (InvalidOperationException e)
        {
            
        }
        
        return value ?? defaultValue;
    }

    private object? SetUserCache(string key, object? value = null)
    {
        _userCache[key] = value;
        return value;
    }
        
    private object? GetUserCache(string key, object? defaultValue = null)
    {
        return _userCache.TryGetValue(key, out var value) ? value : defaultValue;
    }
}