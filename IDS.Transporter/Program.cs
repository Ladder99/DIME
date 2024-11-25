namespace IDS.Transporter;

public static class Program
{
    private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
    private static List<ConnectorRunner> _runners = new();
    public static void Main(string[] args)
    {
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
    }

    private static void Start(string[] args, ManualResetEvent exitEvent)
    {
        Logger.Info("Creating connectors.");
        
        var yaml = Configurator.Configurator.Read(new[] { "config.yaml" });
        var connectors = Configurator.Configurator.CreateConnectors(yaml);
        
        Logger.Info("Creating runners.");

        foreach (var connector in connectors)
        {
            _runners.Add(new ConnectorRunner(connector));
        }
        
        Logger.Info("Starting runners.");
        
        foreach (var runner in _runners)
        {
            runner.Start(exitEvent);
        }
    }
    
    private static void Stop()
    {
        Logger.Info("Stopping runners.");
        foreach (var runner in _runners)
        {
            runner.Stop();
        }
        Logger.Info("Application shutting down gracefully.");
    }
}