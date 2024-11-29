using IDS.Transporter.Configuration;
namespace IDS.Transporter.Connectors;

public abstract class PollingSourceConnector<TConfig, TItem>: SourceConnector<TConfig, TItem>
    where TConfig : ConnectorConfiguration<TItem>
    where TItem : ConnectorItem
{
    public PollingSourceConnector(TConfig configuration, Disruptor.Dsl.Disruptor<MessageBoxMessage> disruptor) : base(configuration, disruptor)
    {
    }

    protected abstract object ReadFromDevice(TItem item);
    
    protected override bool ReadImplementation()
    {
        /*
         * iterate through connector items
         *   execute read from device
         *   execute script
         */
        foreach (var item in Configuration.Items.Where(x => x.Enabled))
        {
            object response = null;
            
            if (item.Address != null)
            {
                response = ReadFromDevice(item);
            }

            if (item.Script != null)
            {
                response = ExecuteScript(response, item.Script);
            }

            if (response != null)
            {
                Samples.Add(new MessageBoxMessage()
                {
                    Path = $"{Configuration.Name}/{item.Name}",
                    Data = response,
                    Timestamp = DateTime.UtcNow.ToEpochMilliseconds()
                });
            }
        }
        
        return true;
    }

}