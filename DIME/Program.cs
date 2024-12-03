using Topshelf;

namespace DIME;

public static class Program
{
    public static void Main(string[] args)
    {
        HostFactory.Run(x =>
        {
            x.Service<TransporterService>(s =>
            {
                s.ConstructUsing(name => new TransporterService());
                s.WhenStarted(tc => tc.Start());
                s.WhenStopped(tc => tc.Stop());
            });
            x.RunAsLocalSystem();
            x.EnableHandleCtrlBreak();
            x.SetServiceName("DIME");
            x.SetDisplayName("DIME");
            x.SetDescription("Data in Motion Enterprise");
            x.UseNLog();
        });
    }
}