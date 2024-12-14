using DIME.Connectors;
using Disruptor.Dsl;

namespace DIME;

public class TransporterService
{
    private readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
    private List<ConnectorRunner> _runners;
    private HttpServer _httpServer;
    private Disruptor<MessageBoxMessage> _disruptor;

    public TransporterService(IConfigurationProvider configurationProvider)
    {
        Logger.Info("Initialize Service.");
        
        var configuration = configurationProvider.GetConfiguration();
        var appConfig = Configurator.AppConfig.Create(configuration);
        
        _runners = new List<ConnectorRunner>();
        _httpServer = new HttpServer(appConfig.ServerUri);
        _disruptor = new(() => new MessageBoxMessage(), appConfig.RingBufferSize);
        
        Logger.Info("Creating connectors.");
        
        var connectors = Configurator.Configurator.CreateConnectors(configuration, _disruptor);
        
        Logger.Info("Creating runners.");
        
        foreach (var connector in connectors)
        {
            _runners.Add(new ConnectorRunner(_runners, connector, _disruptor, _httpServer));
        }
    }
    
    public void AddConnector(IConnector connector)
    {
        _runners.Add(new ConnectorRunner(_runners, connector, _disruptor, _httpServer));
    }
    
    public void Start()
    {
        Logger.Info("Starting DIME");
        
        _httpServer.Start();
        
        // intercept ctrl-c for clean shutdown
        Console.CancelKeyPress += ConsoleOnCancelKeyPress;
        
        Logger.Info("Starting runners.");
        
        foreach (var runner in _runners)
        {
            runner.Start();
        }
        
        Logger.Info("Starting queue.");
        
        _disruptor.Start();
    }

    private void ConsoleOnCancelKeyPress(object? sender, ConsoleCancelEventArgs e)
    {
        Logger.Info("Cancel key sequence intercepted.");
        e.Cancel = true;
    }

    public void Stop()
    {
        Logger.Info("Stopping DIME");
        
        _httpServer.Stop();
        
        Console.CancelKeyPress -= ConsoleOnCancelKeyPress;
        
        Logger.Info("Stopping runners.");
        
        foreach (var runner in _runners)
        {
            runner.Stop();
        }
        
        Logger.Info("Stopping queue.");
        
        _disruptor.Shutdown();
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