using DIME.Configuration.ModbusTcp;
using System.Net.Sockets;
using NModbus;

namespace DIME.Connectors.ModbusTcp;

public class Source: PollingSourceConnector<ConnectorConfiguration, ConnectorItem>
{
    private IModbusMaster _client = null;

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
        var tcpClient = new TcpClient();
        var task = tcpClient.ConnectAsync(
            Configuration.Address,
            Configuration.Port
        );
        task.Wait(Configuration.TimeoutMs);
        if (!tcpClient.Connected)
        {
            return false;
        }
        _client = new ModbusFactory().CreateMaster(tcpClient);
        _client.Transport.ReadTimeout = Configuration.TimeoutMs;
        _client.Transport.WriteTimeout = Configuration.TimeoutMs;
        
        return true;
    }

    protected override object ReadFromDevice(ConnectorItem item)
    {
        object response = null;
            
        switch (item.Type)
        {
            case 1:
                response = _client
                    .ReadCoils(Configuration.Slave, 
                        Convert.ToUInt16(item.Address), 
                        item.Count);
                break;
            case 2:
                response = _client
                    .ReadInputs(Configuration.Slave, 
                        Convert.ToUInt16(item.Address), 
                        item.Count);
                break;
            case 3:
                response = _client
                    .ReadHoldingRegisters(Configuration.Slave, 
                        Convert.ToUInt16(item.Address), 
                        item.Count);
                break;
            case 4:
                response = _client
                    .ReadInputRegisters(Configuration.Slave, 
                        Convert.ToUInt16(item.Address), 
                        item.Count);
                break;
            default:
                response = null;
                break;
        }
        
        return response;
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