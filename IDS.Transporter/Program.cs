using IDS.Transporter.Connectors;
using Disruptor.Dsl;
using Topshelf;

namespace IDS.Transporter;

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
            x.SetServiceName("IDS.Transporter");
            x.SetDisplayName("IDS.Transporter");
            x.SetDescription("Industrial Data Transporter");
            x.UseNLog();
        });
    }
}