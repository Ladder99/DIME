using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using Community.CsharpSqlite;
using DIME.Configuration.SmartPac;
using Newtonsoft.Json;

namespace DIME.Connectors.SmartPac;

public class Source: SourceConnector<ConnectorConfiguration, ConnectorItem>
{
    public Source(ConnectorConfiguration configuration, Disruptor.Dsl.Disruptor<MessageBoxMessage> disruptor) : base(configuration, disruptor)
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
        return new Ping().Send(Configuration.Address, 1000).Status == IPStatus.Success;
    }

    protected override bool ReadImplementation()
    {
        Logger.Trace($"[{Configuration.Name}] PollingSourceConnector:ReadImplementation::ENTER");
        
        if (!string.IsNullOrEmpty(Configuration.LoopEnterScript))
        {
            ExecuteScript(Configuration.LoopEnterScript);
        }
        
        object networkResponse = null;
        string networkRequest = $"\u000201\u0003";
        
        using (TcpClient client = new TcpClient(Configuration.Address, Configuration.Port))
        using (NetworkStream stream = client.GetStream())
        {
            byte[] data = Encoding.ASCII.GetBytes($"{networkRequest}");
            stream.Write(data, 0, data.Length);
            byte[] buffer = new byte[1024];
            int bytesRead = stream.Read(buffer, 0, buffer.Length);
            networkResponse = Encoding.ASCII.GetString(buffer, 0, bytesRead);
            //Console.WriteLine(networkResponse.ToString());
            networkResponse = Regex.Replace(networkResponse.ToString(), @"\p{C}+", string.Empty);
            //Console.WriteLine(networkResponse.ToString());
            networkResponse = networkResponse.ToString().Split(",");
            //Console.WriteLine(networkResponse.ToString());
        }
        
        foreach (var item in Configuration.Items.Where(x => x.Enabled))
        {
            object response = null;
            object readResult = "n/a";
            object scriptResult = "n/a";
            
            if (!string.IsNullOrEmpty(item.Script))
            {
                response = ExecuteScript(networkResponse, item);
                scriptResult = response;
            }
            
            if (response is not null)
            {
                Samples.Add(new MessageBoxMessage()
                {
                    Path = $"{Configuration.Name}/{item.Name}",
                    Data = response,
                    Timestamp = DateTime.UtcNow.ToEpochMilliseconds(),
                    ConnectorItemRef = item
                });
            }
            
            Logger.Trace($"[{Configuration.Name}/{item.Name}] Read Impl. " +
                         $"Read={(readResult==null ? "<null>" : JsonConvert.SerializeObject(readResult))}, " +
                         $"Script={(scriptResult==null ? "<null>" : JsonConvert.SerializeObject(scriptResult))}, " +
                         $"Sample={(response == null ? "DROPPED" : "ADDED")}");
        }
        
        if (!string.IsNullOrEmpty(Configuration.LoopExitScript))
        {
            ExecuteScript(Configuration.LoopExitScript);
        }
        
        Logger.Trace($"[{Configuration.Name}] PollingSourceConnector:ReadImplementation::EXIT");
        
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