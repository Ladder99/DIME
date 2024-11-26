using System.Collections.Concurrent;
using IDS.Transporter.Configuration;

namespace IDS.Transporter.Connectors;

public abstract class SourceConnector<TConfig, TItem>: Connector<TConfig, TItem>, ISourceConnector
    where TConfig : ConnectorConfiguration<TItem>
    where TItem : ConnectorItem
{
    public ConcurrentBag<MessageBoxMessage> Inbox { get; set; }
    public ConcurrentBag<MessageBoxMessage> Samples { get; set; }
    public ConcurrentBag<MessageBoxMessage> Current { get; set; }
    
    public SourceConnector(TConfig configuration, Disruptor.Dsl.Disruptor<MessageBoxMessage> disruptor): base(configuration, disruptor)
    {
        Inbox = new ConcurrentBag<MessageBoxMessage>();
        Samples = new ConcurrentBag<MessageBoxMessage>();
        Current = new ConcurrentBag<MessageBoxMessage>();
    }
    
    protected virtual bool BeforeRead()
    {
        Samples.Clear();
        return true;
    }
    
    protected abstract bool ReadImplementation();

    public virtual bool Read()
    {
        FaultContext = FaultContextEnum.Read;
        
        if (!IsInitialized)
        {
            MarkFaulted(new Exception("Device not initialized."));
            return false;
        }

        if (!IsCreated)
        {
            MarkFaulted(new Exception("Device not created."));
            return false;
        }

        if (!IsConnected)
        {
            MarkFaulted(new Exception("Device not connected."));
            return false;
        }

        try
        {
            BeforeRead();
            var result = ReadImplementation();
            AfterRead();

            if (result)
            {
                ClearFault();
            }
            else
            {
                MarkFaulted(new Exception("Device implementation read failed."));
            }

            return result;
        }
        catch (Exception e)
        {
            MarkFaulted(e);
            Disconnect();
            return false;
        }
    }

    protected virtual bool AfterRead()
    {
        FillInbox();
        PublishInbox();
        
        return true;
    }

    private void FillInbox()
    {
        Inbox.Clear();

        foreach (var sampleResponse in Samples)
        {
            MessageBoxMessage matchingCurrent = null;

            try
            {
                matchingCurrent = Current
                    .Where(x => x.Path == sampleResponse.Path)
                    .First();
            }
            catch (InvalidOperationException e)
            {
            }

            // sample does not exist in current, it is a new sample
            if (matchingCurrent is null)
            {
                Inbox.Add(sampleResponse);
                Current.Add(sampleResponse);
            }
            // sample data is different, it is an updated sample
            else
            {
                if (Configuration.ReportByException)
                {
                    if (!matchingCurrent.Data.Equals(sampleResponse.Data))
                    {
                        Inbox.Add(sampleResponse);
                    }
                }
                else
                {
                    Inbox.Add(sampleResponse);
                }

                matchingCurrent.Data = sampleResponse.Data;
                matchingCurrent.Timestamp = sampleResponse.Timestamp;
            }
        }
    }

    private void PublishInbox()
    {
        if (Inbox.Count > 0)
        {
            using (var scope = Disruptor.RingBuffer.PublishEvents(Inbox.Count))
            {
                int index = 0;
                foreach (var response in Inbox)
                {
                    var data = scope.Event(index);
                    data.Data = response.Data;
                    data.Path = response.Path;
                    data.Timestamp = response.Timestamp;
                    index++;
                }
            }
        }
    }
}