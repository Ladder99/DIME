using DIME.Connectors;
using Disruptor.Dsl;

namespace DIME;

public class TransporterService
{
    private readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
    private List<ConnectorRunner> _runners = new();
    public  Disruptor<MessageBoxMessage> Disruptor { get; private set; } = new(() => new MessageBoxMessage(), 1024);

    public TransporterService(IConfigurationProvider configurationProvider)
    {
        Logger.Info("Creating connectors.");

        var configuration = configurationProvider.GetConfiguration();
        var connectors = Configurator.Configurator.CreateConnectors(configuration, Disruptor);
        
        Logger.Info("Creating runners.");
        
        foreach (var connector in connectors)
        {
            _runners.Add(new ConnectorRunner(_runners, connector, Disruptor));
        }
    }
    
    public void AddConnector(IConnector connector)
    {
        _runners.Add(new ConnectorRunner(_runners, connector, Disruptor));
    }
    
    public void Start()
    {
        Logger.Info("Starting DIME");
        
        // intercept ctrl-c for clean shutdown
        Console.CancelKeyPress += ConsoleOnCancelKeyPress;
        
        Logger.Info("Starting runners.");
        
        foreach (var runner in _runners)
        {
            runner.Start();
        }
        
        Logger.Info("Starting queue.");
        
        Disruptor.Start();
    }

    private void ConsoleOnCancelKeyPress(object? sender, ConsoleCancelEventArgs e)
    {
        Logger.Info("Cancel key sequence intercepted.");
        e.Cancel = true;
    }

    public void Stop()
    {
        Logger.Info("Stopping DIME");
        
        Console.CancelKeyPress -= ConsoleOnCancelKeyPress;
        
        Logger.Info("Stopping runners.");
        
        foreach (var runner in _runners)
        {
            runner.Stop();
        }
        
        Logger.Info("Stopping queue.");
        
        Disruptor.Shutdown();
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