using DIME.Connectors;
using DIME.Configuration.ActiveMq;

namespace DIME.Configurator.ActiveMq;

public static class Source
{
    public static IConnector Create(Dictionary<object, object> section, Disruptor.Dsl.Disruptor<MessageBoxMessage> disruptor)
    {
        ConnectorConfiguration config = ConfigurationHelper<ConnectorConfiguration, ConnectorItem>
            .MakeConfig(section, "ActiveMQ", Configuration.ConnectorDirectionEnum.Source);
        
        config.Address = section.ContainsKey("address") ? Convert.ToString(section["address"]) : string.Empty;
        config.Username = section.ContainsKey("username") ? Convert.ToString(section["username"]) : null;
        config.Password = section.ContainsKey("password") ? Convert.ToString(section["password"]) : null;

        config.Items = ConfigurationHelper<ConnectorConfiguration, ConnectorItem>.MakeItems(config, section, (item, dict) =>
        {
            
        });

        var connector = new Connectors.ActiveMq.Source(config, disruptor);

        return connector;
    }
}