using System.Collections.Concurrent;
using DIME.Configuration;

namespace DIME.Connectors;

public enum FaultContextEnum
{
    None,
    Initialize,
    Create,
    Connect,
    Read,
    Write,
    Disconnect,
    Deinitialize
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
    public ConnectorRunner Runner { get; private set; }
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
        
        Logger.Trace($"[{Configuration.Name}] Connector:.ctor");
    }
    
    protected abstract bool InitializeImplementation();

    public virtual bool Initialize(ConnectorRunner runner)
    {
        Logger.Trace($"[{Configuration.Name}] Connector:Initialize::ENTER");
        
        FaultContext = FaultContextEnum.Initialize;

        bool result = false;
        
        if (IsInitialized)
        {
            MarkFaulted(new Exception("Device already initialized."));
            result = false;
        }
        else
        {
            try
            {
                Runner = runner;
                if (Configuration.Direction == ConnectorDirectionEnum.Sink)
                {
                    Disruptor.HandleEventsWith(new SinkMessageHandler((ISinkConnector)this));
                }
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
            
            result = IsInitialized;
        }
        
        Logger.Trace($"[{Configuration.Name}] Connector:Initialize::EXIT");
        
        return result;
    }
    
    protected abstract bool CreateImplementation();

    public virtual bool Create()
    {
        Logger.Trace($"[{Configuration.Name}] Connector:Create::ENTER");
        
        FaultContext = FaultContextEnum.Create;

        bool result = false;
        
        if (!IsInitialized)
        {
            MarkFaulted(new Exception("Device not initialized."));
            result = false;
        }
        else if (IsCreated)
        {
            MarkFaulted(new Exception("Device already created."));
            result = false;
        }
        else
        {
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
            
            result = IsCreated;
        }
        
        Logger.Trace($"[{Configuration.Name}] Connector:Create::EXIT");
        
        return result;
    }

    public virtual bool BeforeUpdate()
    {
        Logger.Trace($"[{Configuration.Name}] Connector:BeforeUpdate::ENTER");
        
        Logger.Trace($"[{Configuration.Name}] Connector:BeforeUpdate::EXIT");
        
        return true;
    }

    public virtual bool AfterUpdate()
    {
        Logger.Trace($"[{Configuration.Name}] Connector:AfterUpdate::ENTER");
        
        Logger.Trace($"[{Configuration.Name}] Connector:AfterUpdate::EXIT");
        
        return true;
    }
    
    protected abstract bool ConnectImplementation();

    public virtual bool Connect()
    {
        Logger.Trace($"[{Configuration.Name}] Connector:Connect::ENTER");
        
        FaultContext = FaultContextEnum.Connect;
        
        bool result = false;
        
        if (!IsInitialized)
        {
            MarkFaulted(new Exception("Device not initialized."));
            result = false;
        }
        else if (!IsCreated)
        {
            MarkFaulted(new Exception("Device not created."));
            result = false;
        }
        else
        {
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
            
            result = IsConnected;
        }
        
        Logger.Trace($"[{Configuration.Name}] Connector:Connect::EXIT");
        
        return result;
    }
    
    protected abstract bool DisconnectImplementation();

    public virtual bool Disconnect()
    {
        Logger.Trace($"[{Configuration.Name}] Connector:Disconnect::ENTER");
        
        FaultContext = FaultContextEnum.Disconnect;
        
        bool result = false;
        
        if (!IsInitialized)
        {
            MarkFaulted(new Exception("Device not initialized."));
            result = false;
        }
        else if (!IsCreated)
        {
            MarkFaulted(new Exception("Device not created."));
            result = false;
        }
        else
        {
            bool isDisconnected = false;
        
            try
            {
                isDisconnected = DisconnectImplementation();
                IsConnected = !isDisconnected;

                if (isDisconnected)
                {
                    //ClearFault();
                }
                else
                {
                    MarkFaulted(new Exception("Device implementation disconnection failed."));
                }
            
                result = isDisconnected;
            }
            catch (Exception e)
            {
                IsConnected = false;
                MarkFaulted(e);
                result = false;
            }
        }
        
        Logger.Trace($"[{Configuration.Name}] Connector:Disconnect::EXIT");
        
        return result;
    }

    protected abstract bool DeinitializeImplementation();

    public virtual bool Deinitialize()
    {
        Logger.Trace($"[{Configuration.Name}] Connector:Deinitialize::ENTER");
        
        FaultContext = FaultContextEnum.Deinitialize;
        
        bool result = false;
        
        if (!IsInitialized)
        {
            MarkFaulted(new Exception("Device not initialized."));
            result = false;
        }
        else if (!IsCreated)
        {
            MarkFaulted(new Exception("Device not created."));
            result = false;
        }
        else
        {
            try
            { 
                var isDeinitialized = DeinitializeImplementation();

                if (isDeinitialized)
                {
                
                }
                else
                {
                    MarkFaulted(new Exception("Device implementation deinitialization failed."));
                }
            
                result = isDeinitialized;
            }
            catch (Exception e)
            {
                MarkFaulted(e);
                result = false;
            }
        }
        
        Logger.Trace($"[{Configuration.Name}] Connector:Deinitialize::EXIT");
        
        return result;
    }
    
    protected void MarkFaulted(Exception ex)
    {
        Logger.Trace($"[{Configuration.Name}] Connector:MarkFaulted::ENTER");
        
        if(!IsFaulted) Logger.Warn($"[{Configuration.Name}] Fault Set within {FaultContext.ToString()} context. {ex.Message}");
        IsFaulted = true;
        FaultReason = ex;
        
        Logger.Trace($"[{Configuration.Name}] Connector:MarkFaulted::EXIT");
    }

    protected void ClearFault()
    {
        Logger.Trace($"[{Configuration.Name}] Connector:ClearFault::ENTER");
        
        if (IsFaulted)
        {
            Logger.Info($"[{Configuration.Name}] Fault Cleared within {FaultContext.ToString()} context.");
            IsFaulted = false;
            FaultReason = null;
        }
        
        Logger.Trace($"[{Configuration.Name}] Connector:ClearFault::EXIT");

        return;
    }
}