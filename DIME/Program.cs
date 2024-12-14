using Topshelf;
using Topshelf.Logging;
using Topshelf.Runtime;

namespace DIME;

public static class Program
{
    public static void Main(string[] args)
    {
        HostFactory.Run(x =>
        {
            x.Service<DimeService>(s =>
            {
                s.ConstructUsing(name => new DimeService(new FilesystemYamlConfigurationProvider()));
                s.WhenStarted(tc => tc.Startup());
                s.WhenStopped(tc => tc.Shutdown());
            });
            x.RunAsLocalSystem();
            x.EnableHandleCtrlBreak();
            x.SetServiceName("DIME");
            x.SetDisplayName("DIME");
            x.SetDescription("Data in Motion Enterprise");
            x.OnException(ex => HostLogger.Get<DimeService>().Fatal(ex));
            x.UnhandledExceptionPolicy = UnhandledExceptionPolicyCode.LogErrorAndStopService;
            x.UseNLog();
        });
    }
}