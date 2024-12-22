using DIME.Configuration.Console;
using Newtonsoft.Json;

namespace DIME.Connectors.Console;

public class Sink: SinkConnector<ConnectorConfiguration, ConnectorItem>
{
    public Sink(ConnectorConfiguration configuration, Disruptor.Dsl.Disruptor<MessageBoxMessage> disruptor) : base(configuration, disruptor)
    {
    }

    protected override bool InitializeImplementation()
    {
        return true;
    }

    protected override bool CreateImplementation()
    {
        return true;
    }

    protected override bool ConnectImplementation()
    {
        return true;
    }

    protected override bool WriteImplementation()
    {
        foreach (var message in Outbox)
        {
            System.Console.ForegroundColor = ConsoleColor.Cyan;
            System.Console.WriteLine(
                $"[{Configuration.Name}] " +
                $"Path: {message.Path}, " +
                $"Message: {(Configuration.UseSinkTransform ? TransformAndSerializeMessage(message) : JsonConvert.SerializeObject(message))}");
            System.Console.ResetColor();
        }
        
        return true;
    }

    protected override bool DisconnectImplementation()
    {
        return true;
    }
    
    protected override bool DeinitializeImplementation()
    {
        return true;
    }
}