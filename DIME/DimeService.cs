using DIME.Connectors;
using Disruptor.Dsl;
using ConsoleCancelEventArgs = System.ConsoleCancelEventArgs;

namespace DIME;

public class DimeService
{
    private readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
    private readonly List<ConnectorRunner> _runners = new List<ConnectorRunner>();
    private IConfigurationProvider _configurationProvider;
    private AdminServer _httpServer;
    private Disruptor<MessageBoxMessage> _disruptor;
    private bool _isRunning = false;
    private List<IConnector> _externalConnectors = new List<IConnector>();

    public DimeService(IConfigurationProvider configurationProvider)
    {
        _configurationProvider = configurationProvider;
    }
    
    /// <summary>
    /// Add an external connector instance.
    /// External connector instance must be added before Startup() is invoked.
    /// </summary>
    /// <param name="connector">External connector instance.</param>
    /// <returns>Whether the external connector instance was successfully added or not.</returns>
    public bool AddConnector(IConnector connector)
    {
        if (_isRunning) return false;
        _externalConnectors.Add(connector);
        return true;
    }
    
    /// <summary>
    /// Startup and run the DIME service.
    /// </summary>
    public void Startup()
    {
        Logger.Info("Starting DIME");
        Console.CancelKeyPress += ConsoleOnCancelKeyPress;
        
        var dictionaryConfiguration = _configurationProvider.ReadConfiguration().Item2;
        var appConfig = Configurator.AppConfig.Create(dictionaryConfiguration);
        _httpServer = new AdminServer(this, _configurationProvider, appConfig.HttpServerUri, appConfig.WsServerUri);
        
        Start();
        _httpServer.Start();
    }

    /// <summary>
    /// Shutdown the DIME service.
    /// </summary>
    public void Shutdown()
    {
        Logger.Info("Stopping DIME");
        Console.CancelKeyPress -= ConsoleOnCancelKeyPress;
        
        _httpServer.Stop();
        Stop();
    }

    /// <summary>
    /// Restart all internal and external connectors.
    /// </summary>
    public void Restart()
    {
        Logger.Info("Restarting DIME");
        Stop();
        Start();
    }
    
    private void Start()
    {
        _isRunning = true;
        var dictionaryConfiguration = _configurationProvider.ReadConfiguration().Item2;
        var appConfig = Configurator.AppConfig.Create(dictionaryConfiguration);
        _disruptor = new(() => new MessageBoxMessage(), appConfig.RingBufferSize);
        
        Logger.Info("Creating connectors.");
        var connectors = Configurator.Configurator.CreateConnectors(dictionaryConfiguration, _disruptor);
        
        Logger.Info("Creating runners for internal connectors.");
        foreach (var connector in connectors)
        {
            _runners.Add(new ConnectorRunner(_runners, connector, _disruptor, _httpServer));
        }

        Logger.Info("Creating runners for external connectors.");
        foreach (var connector in _externalConnectors)
        {
            _runners.Add(new ConnectorRunner(_runners, connector, _disruptor, _httpServer));
        }
        
        Logger.Info("Starting runners.");
        foreach (var runner in _runners)
        {
            runner.Start();
        }
        
        _disruptor.Start();
    }

    private void Stop()
    {
        Logger.Info("Stopping runners.");
        foreach (var runner in _runners)
        {
            runner.Stop();
        }
        _runners.Clear();
        
        _disruptor.Shutdown(); ;
        _isRunning = false;
    }
    
    private void ConsoleOnCancelKeyPress(object? sender, ConsoleCancelEventArgs e)
    {
        Logger.Info("Cancel key sequence intercepted.");
        e.Cancel = true;
    }
}

/*
 ConnectorRunner
  .Start
    Connector
      .Initialize
      .InitializeImplementation
      .Create
      .CreateImplementation
  .Execute
    Connector
      .BeforeUpdate
      .Connect
      .ConnectImplementation
      .Read
      .ReadImplementation
      .Write
      .WriteImplementation
      .AfterUpdate
  .Stop
    Connector
      .Disconnect
      .DisconnectImplementation
      .Deinitialize
      .DeinitializeImplementation
*/