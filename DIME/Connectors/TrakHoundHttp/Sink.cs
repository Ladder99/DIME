using DIME.Configuration.TrakHoundHttp;
using TrakHound;
using TrakHound.Clients;
using TrakHound.Entities;
using TrakHound.Requests;

namespace DIME.Connectors.TrakHoundHttp;

public class Sink: SinkConnector<ConnectorConfiguration, ConnectorItem>
{
    private readonly TrakHoundOnChangeFilter _filter;
    private TrakHoundHttpPublishStreamClient _trakhoundClient;


    public Sink(ConnectorConfiguration configuration, Disruptor.Dsl.Disruptor<MessageBoxMessage> disruptor) : base(configuration, disruptor)
    {
        _filter = new TrakHoundOnChangeFilter();
    }

    protected override bool InitializeImplementation()
    {
        _filter.Clear();
        return true;
    }

    protected override bool CreateImplementation()
    {
        var clientConfiguration = new TrakHoundHttpClientConfiguration();
        clientConfiguration.Hostname = Configuration.Address;
        clientConfiguration.Port = Configuration.Port;
        clientConfiguration.UseSSL = Configuration.UseSsl;
        clientConfiguration.Path = Configuration.HostPath;

        var routerId = Configuration.Router;

        _trakhoundClient = new TrakHoundHttpPublishStreamClient(clientConfiguration, routerId);
        return true;
    }

    protected override bool ConnectImplementation()
    {
        if (_trakhoundClient != null) _trakhoundClient.Connect();
        return true;
    }

    protected override bool WriteImplementation()
    { 
        if (_trakhoundClient != null)
        {
            foreach (var message in Outbox)
            {
                if (!string.IsNullOrEmpty(message.Path) && message.Data != null)
                {
                    var value = message.Data.ToString(); // Need to take into account Arrays

                    if (_filter.Filter(message.Path, value))
                    {
                        var observation = new TrakHoundObservationEntry();
                        observation.ObjectPath = GetMessagePath(message.Path);
                        observation.Value = value;
                        observation.DataType = value.IsNumeric() ? TrakHoundObservationDataType.Double : TrakHoundObservationDataType.String;
                        observation.Timestamp = UnixTimeExtensions.FromUnixTimeMilliseconds(message.Timestamp);

                        _trakhoundClient.Publish(observation);
                    }
                }
            }
        }
        
        return true;
    }

    protected override bool DisconnectImplementation()
    {
        if (_trakhoundClient != null) _trakhoundClient.Disconnect();
        return true;
    }
    
    protected override bool DeinitializeImplementation()
    {
        if (_trakhoundClient != null) _trakhoundClient.Dispose();
        return true;
    }


    private string GetMessagePath(string messagePath)
    {
        if (messagePath != null)
        {
            if (IsSystemMessage(messagePath))
            {
                return TrakHoundPath.Combine("Ladder99:/.DIME/ModuleSink", messagePath);
            }
            else
            {
                // Remove Module Name
                var path = Url.RemoveFirstFragment(messagePath);

                // Add BasePath from Configuration
                if (!string.IsNullOrEmpty(Configuration.BasePath)) path = TrakHoundPath.Combine(Configuration.BasePath, path);

                return path;
            }
        }

        return null;
    }

    private static bool IsSystemMessage(string messagePath)
    {
        if (messagePath != null)
        {
            var path = Url.RemoveFirstFragment(messagePath);
            var name = Url.GetFirstFragment(path);
            return name == "$SYSTEM";
        }

        return false;
    }
}