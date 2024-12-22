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
            ExecuteScript(Configuration.LoopEnterScript, this);
        }
        
        object networkResponse01 = null;
        string networkRequest01 = $"\u000201\u0003";
        
        object networkResponse03 = null;
        string networkRequest03 = $"\u000203\u0003";
        
        using (TcpClient client = new TcpClient(Configuration.Address, Configuration.Port))
        using (NetworkStream stream = client.GetStream())
        {
            byte[] data01 = Encoding.ASCII.GetBytes($"{networkRequest01}");
            stream.Write(data01, 0, data01.Length);
            byte[] buffer01 = new byte[1024];
            int bytesRead01 = stream.Read(buffer01, 0, buffer01.Length);
            networkResponse01 = Encoding.ASCII.GetString(buffer01, 0, bytesRead01);
            networkResponse01 = Regex.Replace(networkResponse01.ToString(), @"\p{C}+", string.Empty);
            networkResponse01 = networkResponse01.ToString().Split(",");
            
            byte[] data03 = Encoding.ASCII.GetBytes($"{networkRequest03}");
            stream.Write(data03, 0, data03.Length);
            byte[] buffer03 = new byte[1024];
            int bytesRead03 = stream.Read(buffer03, 0, buffer03.Length);
            networkResponse03 = Encoding.ASCII.GetString(buffer03, 0, bytesRead03);
            networkResponse03 = Regex.Replace(networkResponse03.ToString(), @"\p{C}+", string.Empty);
            networkResponse03 = networkResponse03.ToString().Split(",");
        }
        
        List<string> networkResponse = new[]
        {
            (string[])networkResponse01, 
            (string[])networkResponse03
        }.SelectMany(x => x).ToList();

        System.Console.WriteLine(networkResponse);
        
        foreach (var item in Configuration.Items.Where(x => x.Enabled))
        {
            object response = null;
            object readResult = "n/a";
            object scriptResult = "n/a";
            
            if (ItemOrConfigurationHasItemScript(item))
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
            ExecuteScript(Configuration.LoopExitScript, this);
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