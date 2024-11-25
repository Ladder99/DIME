using System.Collections.Concurrent;
using IDS.Transporter.Configuration;

namespace IDS.Transporter.Connectors;

public abstract class SourceConnector<TConfig, TItem>: Connector<TConfig, TItem>, ISourceConnector
    where TConfig : ConnectorConfiguration<TItem>
    where TItem : ConnectorItem
{
    public ConcurrentBag<BoxMessage> Inbox { get; set; }
    public ConcurrentBag<BoxMessage> SampleReadResponses { get; set; }
    public ConcurrentBag<BoxMessage> CurrentReadResponses { get; set; }
    
    public SourceConnector(TConfig configuration, Disruptor.Dsl.Disruptor<BoxMessage> disruptor): base(configuration, disruptor)
    {
        Inbox = new ConcurrentBag<BoxMessage>();
        SampleReadResponses = new ConcurrentBag<BoxMessage>();
        CurrentReadResponses = new ConcurrentBag<BoxMessage>();
    }
    
    protected virtual bool BeforeRead()
    {
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

        foreach (var sampleResponse in SampleReadResponses)
        {
            BoxMessage matchingCurrent = null;

            try
            {
                matchingCurrent = CurrentReadResponses
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
                CurrentReadResponses.Add(sampleResponse);
            }
            // sample data is different, it is an updated sample
            else
            {
                if (!matchingCurrent.Data.Equals(sampleResponse.Data))
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