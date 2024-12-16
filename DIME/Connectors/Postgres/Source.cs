using System.Data;
using DIME.Configuration.Postgres;
using Npgsql;

namespace DIME.Connectors.Postgres;

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
        using (var connection = new NpgsqlConnection(Configuration.ConnectionString))
        {
            connection.Open();
            var cmd = new NpgsqlCommand(Configuration.CommandText, connection);
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