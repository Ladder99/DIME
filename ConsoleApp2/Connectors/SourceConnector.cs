using ConsoleApp2.Configuration;

namespace ConsoleApp2.Connectors;

public abstract class SourceConnector<TConfig, TItem> 
    where TConfig : ConnectorConfiguration<TItem>
    where TItem : ConnectorItem
{
    protected readonly NLog.Logger Logger;
    protected TConfig Configuration { get; set; }
    
    protected PropertyBag Properties { get; set; }
    
    public List<ReadResponse> DeltaReadResponses { get; set; }
    public List<ReadResponse> SampleReadResponses { get; set; }
    public List<ReadResponse> CurrentReadResponses { get; set; }
    
    public bool IsInitialized { get; private set; }
    
    public bool IsCreated { get; private set; }
    
    public bool IsConnected { get; protected set; }

    protected SourceConnector()
    {
        Logger = NLog.LogManager.GetLogger(GetType().FullName);
        Properties = new PropertyBag();
        DeltaReadResponses = new List<ReadResponse>();
        SampleReadResponses = new List<ReadResponse>();
        CurrentReadResponses = new List<ReadResponse>();
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

    public virtual bool BeforeRead()
    {
        return true;
    }
    
    protected abstract bool ReadImplementation();

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
            var result = ReadImplementation();
        }
        catch (Exception e)
        {
            Disconnect();
        }
        
        return true;
    }

    protected virtual bool UpdateReadResponses()
    {
        DeltaReadResponses.Clear();

        foreach (var sampleResponse in SampleReadResponses)
        {
            var matchingCurrent = CurrentReadResponses.Find(x => x.Path == sampleResponse.Path);
            // sample does not exist in current, it is a new sample
            if (matchingCurrent is null)
            {
                DeltaReadResponses.Add(sampleResponse);
                CurrentReadResponses.Add(sampleResponse);
            }
            // sample data is different, it is an updated sample
            else
            {
                if (!matchingCurrent.Data.Equals(sampleResponse.Data))
                {
                    DeltaReadResponses.Add(sampleResponse);
                }
                
                matchingCurrent.Data = sampleResponse.Data;
                matchingCurrent.Timestamp = sampleResponse.Timestamp;
            }
        }
        
        return true;
    }
    
    public virtual bool AfterRead()
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