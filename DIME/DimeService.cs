using DIME.Connectors;
using Disruptor.Dsl;
using ConsoleCancelEventArgs = System.ConsoleCancelEventArgs;

namespace DIME;

public class DimeService
{
    private readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
    private readonly List<ConnectorRunner> _runners = new List<ConnectorRunner>();
    private IConfigurationProvider _configurationProvider;
    private HttpServer _httpServer;
    private Disruptor<MessageBoxMessage> _disruptor;

    public DimeService(IConfigurationProvider configurationProvider)
    {
        _configurationProvider = configurationProvider;
    }
    
    public void AddConnector(IConnector connector)
    {
        _runners.Add(new ConnectorRunner(_runners, connector, _disruptor, _httpServer));
    }

    public void Startup()
    {
        Logger.Info("Starting DIME");
        Console.CancelKeyPress += ConsoleOnCancelKeyPress;
        
        var dictionaryConfiguration = _configurationProvider.ReadConfiguration().Item2;
        var appConfig = Configurator.AppConfig.Create(dictionaryConfiguration);
        _httpServer = new HttpServer(this, _configurationProvider, appConfig.ServerUri);
        
        Start();
        _httpServer.Start();
    }

    public void Shutdown()
    {
        Logger.Info("Stopping DIME");
        Console.CancelKeyPress -= ConsoleOnCancelKeyPress;
        
        _httpServer.Stop();
        Stop();
    }
    
    public void Start()
    {
        var dictionaryConfiguration = _configurationProvider.ReadConfiguration().Item2;
        var appConfig = Configurator.AppConfig.Create(dictionaryConfiguration);
        _disruptor = new(() => new MessageBoxMessage(), appConfig.RingBufferSize);
        
        Logger.Info("Creating connectors.");
        var connectors = Configurator.Configurator.CreateConnectors(dictionaryConfiguration, _disruptor);
        
        Logger.Info("Creating runners.");
        foreach (var connector in connectors)
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

    public void Stop()
    {
        Logger.Info("Stopping runners.");
        foreach (var runner in _runners)
        {
            runner.Stop();
        }
        _runners.Clear();
        
        _disruptor.Shutdown();
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