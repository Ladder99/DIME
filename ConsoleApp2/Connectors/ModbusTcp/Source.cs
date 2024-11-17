using System.Net.Sockets;
using ConsoleApp2.Configuration;
using libplctag;
using libplctag.DataTypes;
using NLog;
using NModbus;

namespace ConsoleApp2.Connectors.ModbusTcp;

public class Source: SourceConnector<ModbusTcpConnectorConfiguration, ModbusTcpConnectorItem>
{
    private IModbusMaster _client = null;
    
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
        var tcpClient = new TcpClient();
        var task = tcpClient.ConnectAsync(
            Configuration.IpAddress,
            Configuration.Port
        );
        task.Wait(Configuration.Timeout);
        if (!tcpClient.Connected)
        {
            return false;
        }
        _client = new ModbusFactory().CreateMaster(tcpClient);
        _client.Transport.ReadTimeout = Configuration.Timeout;
        _client.Transport.WriteTimeout = Configuration.Timeout;
        
        return true;
    }

    protected override bool ReadImplementation()
    {
        SampleReadResponses.Clear();
        
        foreach (var item in Configuration.Items)
        {
            object response = null;
            
            switch (item.Type)
            {
                case 1:
                    response = _client
                        .ReadCoils(Configuration.Slave, 
                            item.Address, 
                            item.Count);
                    break;
                case 2:
                    response = _client
                        .ReadInputs(Configuration.Slave, 
                            item.Address, 
                            item.Count);
                    break;
                case 3:
                    response = _client
                        .ReadHoldingRegisters(Configuration.Slave, 
                            item.Address, 
                            item.Count);
                    break;
                case 4:
                    response = _client
                        .ReadInputRegisters(Configuration.Slave, 
                            item.Address, 
                            item.Count);
                    break;
                default:
                    response = null;
                    break;
            }
            
            
            SampleReadResponses.Add(new ReadResponse()
            {
                Path = $"{Configuration.Name}/{item.Name}",
                Data = response,
                Timestamp = DateTime.UtcNow.ToEpochMilliseconds()
            });
        }

        UpdateReadResponses();
        
        return true;
    }

    protected override bool DisconnectImplementation()
    {
        return true;
    }
}