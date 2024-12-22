using System.Text;
using DIME.Configuration.SplunkHec;
using Newtonsoft.Json;

namespace DIME.Connectors.SplunkHec;

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
        client.DefaultRequestHeaders.Add("Authorization", $"Splunk {Configuration.Token}");
        
        foreach (var message in Outbox)
        {
            bool doSend = false;
            StringContent content = null;
            
            if (Configuration.EventOrMetric.ToLower() == "event")
            {
                var payload = new
                {
                    time = message.Timestamp,
                    host = message.ConnectorItemRef is null ? Configuration.Name : message.ConnectorItemRef.Configuration.Name,
                    sourcetype = Configuration.SourceType,
                    source = Configuration.Source,
                    @event = new
                    {
                        key = message.Path,
                        value = Configuration.UseSinkTransform ? TransformAndSerializeMessage(message) : message.Data
                    }
                };
                
                content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
                doSend = true;
            }
            else if (Configuration.EventOrMetric.ToLower() == "metric")
            {
                var payload = new
                {
                    time = message.Timestamp,
                    host = message.ConnectorItemRef is null ? Configuration.Name : message.ConnectorItemRef.Configuration.Name,
                    sourcetype = Configuration.SourceType,
                    source = Configuration.Source,
                    @event = "metric",
                    fields = new
                    {
                        metric_name = message.Path,
                        _value = Configuration.UseSinkTransform ? TransformAndSerializeMessage(message) : message.Data
                    }
                };
                
                content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
                doSend = true;
            }
            
            if (doSend)
            {
                var response = client
                    .PostAsync($"http{(Configuration.UseSsl ? "s" : string.Empty)}://{Configuration.Address}:{Configuration.Port}/services/collector", content)
                    .GetAwaiter().GetResult();
                response.EnsureSuccessStatusCode();
            }
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