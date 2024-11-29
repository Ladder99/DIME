using IDS.Transporter.Connectors;
using Disruptor.Dsl;

namespace IDS.Transporter;

public static class Program
{
    private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
    private static List<ConnectorRunner> _runners = new();
    private static Disruptor<MessageBoxMessage> _disruptor = new(() => new MessageBoxMessage(), 1024);
    
    public static void Main(string[] args)
    {
        Logger.Info("Starting IDS.Transporter");
        
        // set working directory
        Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);

        // intercept ctrl-c for clean shutdown
        var exitEvent = new ManualResetEvent(false);
        Console.CancelKeyPress += (sender, eventArgs) =>
        {
            Logger.Info("Cancel key sequence intercepted.");
            eventArgs.Cancel = true;
            exitEvent.Set();
        };
        
        Start(args, exitEvent);
        // wait for ctrl-c
        exitEvent.WaitOne();
        Stop();
        
        Logger.Info("Stopping IDS.Transporter");
    }

    private static void Start(string[] args, ManualResetEvent exitEvent)
    {
        Logger.Info("Creating connectors.");

        var configFiles = Directory.GetFiles("./Configs", "*.yaml");
        configFiles = configFiles.Where(x => !x.EndsWith("main.yaml"))
            .Concat(configFiles.Where(x => x.EndsWith("main.yaml")))
            .ToArray();
        var yaml = Configurator.Configurator.Read(configFiles);
        var connectors = Configurator.Configurator.CreateConnectors(yaml, _disruptor);
        
        Logger.Info("Creating runners.");

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
         */
        
        foreach (var connector in connectors)
        {
            _runners.Add(new ConnectorRunner(connector, _disruptor));
        }
        
        Logger.Info("Starting runners.");
        
        foreach (var runner in _runners)
        {
            runner.Start(exitEvent);
        }
        
        Logger.Info("Starting queue.");
        
        _disruptor.Start();
    }
    
    private static void Stop()
    {
        Logger.Info("Stopping runners.");
        foreach (var runner in _runners)
        {
            runner.Stop();
        }
    }
}