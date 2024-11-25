using IDS.Transporter.Configuration;

namespace IDS.Transporter.Connectors;

public abstract class SinkConnector<TConfig, TItem> 
    where TConfig : ConnectorConfiguration<TItem>
    where TItem : ConnectorItem
{
    protected readonly NLog.Logger Logger;
    protected TConfig Configuration { get; set; }
    
    protected PropertyBag Properties { get; set; }
    
    public bool IsInitialized { get; private set; }
    
    public bool IsCreated { get; private set; }
    
    public bool IsConnected { get; protected set; }

    protected SinkConnector()
    {
        Logger = NLog.LogManager.GetLogger(GetType().FullName);
        Properties = new PropertyBag();
    }
    
    protected abstract bool InitializeImplementation();

    public virtual bool Initialize(TConfig configuration)
    {
        if (IsInitialized)
        {
            return false;
        }
        
        Configuration = configuration;
        
        IsInitialized = InitializeImplementation();
        return IsInitialized;
    }
    
    protected abstract bool CreateImplementation();

    public virtual bool Create()
    {
        if (!IsInitialized)
        {
            return false;
        }

        if (IsCreated)
        {
            return false;
        }
        
        IsCreated = CreateImplementation();
        return IsCreated;
    }
    
    protected abstract bool ConnectImplementation();

    public virtual bool Connect()
    {
        if (!IsInitialized)
        {
            return false;
        }

        if (!IsCreated)
        {
            return false;
        }
        
        IsConnected = ConnectImplementation();
        return IsConnected;
    }

    public virtual bool BeforeWrite()
    {
        return true;
    }
    
    protected abstract bool WriteImplementation();

    public virtual bool Read()
    {
        if (!IsInitialized)
        {
            return false;
        }

        if (!IsCreated)
        {
            return false;
        }

        if (!IsConnected)
        {
            return false;
        }

        try
        {
            var result = WriteImplementation();
        }
        catch (Exception e)
        {
            Disconnect();
        }
        
        return true;
    }
    
    public virtual bool AfterWrite()
    {
        return true;
    }
    
    protected abstract bool DisconnectImplementation();

    public virtual bool Disconnect()
    {
        if (!IsInitialized)
        {
            return false;
        }

        if (!IsCreated)
        {
            return false;
        }
        
        IsConnected = !DisconnectImplementation();
        return !IsConnected;
    }
}