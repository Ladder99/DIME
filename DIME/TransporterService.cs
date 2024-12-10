using DIME.Connectors;
using Disruptor.Dsl;

namespace DIME;

public class TransporterService
{
    private readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
    private List<ConnectorRunner> _runners = new();
    private Disruptor<MessageBoxMessage> _disruptor = new(() => new MessageBoxMessage(), 1024);
    private IConfigurationProvider _configurationProvider = null;
    
    public void Start(IConfigurationProvider configurationProvider)
    {
        Logger.Info("Starting DIME");
        
        _configurationProvider = configurationProvider;
        
        // intercept ctrl-c for clean shutdown
        Console.CancelKeyPress += (sender, eventArgs) =>
        {
            Logger.Info("Cancel key sequence intercepted.");
            eventArgs.Cancel = true;
        };
        
        Logger.Info("Creating connectors.");

        var configuration = configurationProvider.GetConfiguration();
        var connectors = Configurator.Configurator.CreateConnectors(configuration, _disruptor);
        
        Logger.Info("Creating runners.");

        foreach (var connector in connectors)
        {
            _runners.Add(new ConnectorRunner(_runners, connector, _disruptor));
        }
        
        Logger.Info("Starting runners.");
        
        foreach (var runner in _runners)
        {
            runner.Start();
        }
        
        Logger.Info("Starting queue.");
        
        _disruptor.Start();
    }

    public void Stop()
    {
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