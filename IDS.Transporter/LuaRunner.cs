using IDS.Transporter.Configuration;

namespace IDS.Transporter;

public class LuaRunner
{
    private readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
    
    private NLua.Lua _state;
    
    public bool Initialize(IConnectorConfiguration connectorConfiguration)
    {
        try
        {
            _state = new NLua.Lua();
            _state.LoadCLRPackage();
            _state["result"] = null;
            _state.DoString(connectorConfiguration.InitScript ?? "");
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

    public object[] DoString(string chunk)
    {
        return _state.DoString(chunk);
    }
}