using IDS.Transporter.Configuration;

namespace IDS.Transporter.Connectors;

public enum FaultContextEnum
{
    None,
    Initialize,
    Create,
    Connect,
    Read,
    Write,
    Disconnect
}

public abstract class SourceConnector<TConfig, TItem>: IConnector
    where TConfig : ConnectorConfiguration<TItem>
    where TItem : ConnectorItem
{
    protected readonly NLog.Logger Logger;
    public FaultContextEnum FaultContext { get; set; }
    public bool IsFaulted {get; private set;}
    public Exception FaultReason { get; set; }
    public TConfig Configuration { get; set; }
    protected PropertyBag Properties { get; set; }
    public List<ReadResponse> DeltaReadResponses { get; set; }
    public List<ReadResponse> SampleReadResponses { get; set; }
    public List<ReadResponse> CurrentReadResponses { get; set; }
    public bool IsInitialized { get; private set; }
    public bool IsCreated { get; private set; }
    public bool IsConnected { get; protected set; }

    public SourceConnector(TConfig configuration)
    {
        Logger = NLog.LogManager.GetLogger(GetType().FullName);
        Configuration = configuration;
        Properties = new PropertyBag();
        DeltaReadResponses = new List<ReadResponse>();
        SampleReadResponses = new List<ReadResponse>();
        CurrentReadResponses = new List<ReadResponse>();
    }
    
    protected abstract bool InitializeImplementation();

    public virtual bool Initialize()
    {
        FaultContext = FaultContextEnum.None;
        
        if (IsInitialized)
        {
            MarkFaulted(new Exception("Device already initialized."));
            return false;
        }

        try
        {
            IsInitialized = InitializeImplementation();

            if (IsInitialized)
            {
                ClearFault();
            }
            else
            {
                MarkFaulted(new Exception("Device implementation initialization failed."));
            }
        }
        catch (Exception e)
        {
            IsInitialized = false;

            MarkFaulted(e);
        }
        
        return IsInitialized;
    }
    
    protected abstract bool CreateImplementation();

    public virtual bool Create()
    {
        FaultContext = FaultContextEnum.Initialize;
        
        if (!IsInitialized)
        {
            MarkFaulted(new Exception("Device not initialized."));
            return false;
        }

        if (IsCreated)
        {
            MarkFaulted(new Exception("Device already created."));
            return false;
        }
        
        try
        {
            IsCreated = CreateImplementation();

            if (IsCreated)
            {
                ClearFault();
            }
            else
            {
                MarkFaulted(new Exception("Device implementation creation failed."));
            }
        }
        catch (Exception e)
        {
            IsCreated = false;

            MarkFaulted(e);
        }
        
        return IsCreated;
    }
    
    protected abstract bool ConnectImplementation();

    public virtual bool Connect()
    {
        FaultContext = FaultContextEnum.Connect;
        
        if (!IsInitialized)
        {
            MarkFaulted(new Exception("Device not initialized."));
            return false;
        }

        if (!IsCreated)
        {
            MarkFaulted(new Exception("Device not created."));
            return false;
        }
        
        try
        {
            IsConnected = ConnectImplementation();

            if (IsConnected)
            {
                ClearFault();
            }
            else
            {
                MarkFaulted(new Exception("Device implementation connection failed."));
            }
        }
        catch (Exception e)
        {
            IsConnected = false;

            MarkFaulted(e);
        }
        
        return IsConnected;
    }

    public virtual bool BeforeRead()
    {
        return true;
    }
    
    protected abstract bool ReadImplementation();

    public virtual bool Read()
    {
        FaultContext = FaultContextEnum.Read;
        
        if (!IsInitialized)
        {
            MarkFaulted(new Exception("Device not initialized."));
            return false;
        }

        if (!IsCreated)
        {
            MarkFaulted(new Exception("Device not created."));
            return false;
        }

        if (!IsConnected)
        {
            MarkFaulted(new Exception("Device not connected."));
            return false;
        }

        try
        {
            var result = ReadImplementation();

            if (result)
            {
                ClearFault();
            }
            else
            {
                MarkFaulted(new Exception("Device implementation read failed."));
            }

            return result;
        }
        catch (Exception e)
        {
            Disconnect();
            MarkFaulted(e);
            return false;
        }
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
        FaultContext = FaultContextEnum.Disconnect;
        
        if (!IsInitialized)
        {
            MarkFaulted(new Exception("Device not initialized."));
            return false;
        }

        if (!IsCreated)
        {
            MarkFaulted(new Exception("Device not created."));
            return false;
        }

        bool isDisconnected = false;
        
        try
        {
            isDisconnected = DisconnectImplementation();
            IsConnected = !isDisconnected;

            if (isDisconnected)
            {
                ClearFault();
            }
            else
            {
                MarkFaulted(new Exception("Device implementation disconnection failed."));
            }
            
            return isDisconnected;
        }
        catch (Exception e)
        {
            IsConnected = false;
            MarkFaulted(e);
            return false;
        }
    }
    
    protected void MarkFaulted(Exception ex)
    {
        if(!IsFaulted) Logger.Warn($"Fault Set within {FaultContext.ToString()} context. {ex.Message}");
        IsFaulted = true;
        FaultReason = ex;
    }

    protected void ClearFault()
    {
        if (!IsFaulted)
        {
            return;
        }
        Logger.Info($"Fault Cleared within {FaultContext.ToString()} context.");
        IsFaulted = false;
        FaultReason = null;
    }
}