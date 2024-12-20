using System.Text;
using DIME.Configuration.HttpClient;
using Newtonsoft.Json;

namespace DIME.Connectors.HttpClient;

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
        var client = new System.Net.Http.HttpClient();
        var contentType = "text/plain";

        foreach (var header in Configuration.Headers)
        {
            if (header.Key.ToLower() != "content-type")
            {
                client.DefaultRequestHeaders.Add(header.Key, header.Value);
            }
            else
            {
                contentType = header.Value;
            }
            
        }
        
        foreach (var message in Outbox)
        {
           var response = client
                .PostAsync($"{Configuration.Uri}", new StringContent(TransformAndSerializeMessage(message), Encoding.UTF8, contentType))
                .GetAwaiter().GetResult();
            response.EnsureSuccessStatusCode();
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