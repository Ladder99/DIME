using System.Data;
using DIME.Configuration.MsSql;
using Microsoft.Data.SqlClient;

namespace DIME.Connectors.MsSql;

public class Source: DatabaseSourceConnector<ConnectorConfiguration, ConnectorItem>
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
        return true;
    }

    protected override DataTable ReadFromDevice()
    {
        using (var connection = new SqlConnection(Configuration.ConnectionString))
        {
            connection.Open();
            var cmd = new SqlCommand(Configuration.CommandText, connection);
            var reader = cmd.ExecuteReader();
            var table = new DataTable();
            table.Load(reader);
            return table;
        }
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