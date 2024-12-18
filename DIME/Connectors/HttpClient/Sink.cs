using System.Text;
using DIME.Configuration.HttpClient;

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
        //client.DefaultRequestHeaders.Add("Authorization", $"some value");
        
        foreach (var message in Outbox)
        {
            var transformedMessage = TransformMessage(message);
            StringContent content = new StringContent(transformedMessage.ToString(), Encoding.UTF8, "application/json");
            
            var response = client
                .PostAsync($"{Configuration.Uri}", content)
                .GetAwaiter().GetResult();
            response.EnsureSuccessStatusCode();
        }
        
        Dictionary<string,string> d = new Dictionary<string, string>();

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