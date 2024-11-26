using System.Collections.Concurrent;
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

public abstract class Connector<TConfig, TItem>: IConnector
    where TConfig : ConnectorConfiguration<TItem>
    where TItem : ConnectorItem
{
    IConnectorConfiguration IConnector.Configuration 
    {
        get
        {
            return Configuration;
        }
    }
    
    protected readonly NLog.Logger Logger;
    public FaultContextEnum FaultContext { get; set; }
    public bool IsFaulted {get; private set;}
    public Exception FaultReason { get; set; }
    public TConfig Configuration { get; set; }
    protected PropertyBag Properties { get; set; }
    protected Disruptor.Dsl.Disruptor<MessageBoxMessage> Disruptor { get; set; }
    public bool IsInitialized { get; private set; }
    public bool IsCreated { get; private set; }
    public bool IsConnected { get; protected set; }

    public Connector(TConfig configuration, Disruptor.Dsl.Disruptor<MessageBoxMessage> disruptor)
    {
        Logger = NLog.LogManager.GetLogger(GetType().FullName);
        Configuration = configuration;
        Properties = new PropertyBag();
        Disruptor = disruptor;
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
        if(!IsFaulted) Logger.Warn($"[{Configuration.Name}] Fault Set within {FaultContext.ToString()} context. {ex.Message}");
        IsFaulted = true;
        FaultReason = ex;
    }

    protected void ClearFault()
    {
        if (!IsFaulted)
        {
            return;
        }
        Logger.Info($"[{Configuration.Name}] Fault Cleared within {FaultContext.ToString()} context.");
        IsFaulted = false;
        FaultReason = null;
    }
}