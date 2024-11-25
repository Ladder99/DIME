namespace IDS.Transporter;

using System.Collections.Concurrent;
using System.Collections.Generic;

public class WideQueue<T>
{
    private readonly ConcurrentQueue<T> _queue = new ConcurrentQueue<T>();
    private readonly List<BlockingCollection<T>> _subscribers = new List<BlockingCollection<T>>();
    private readonly object _subscriberLock = new object();

    public void Enqueue(T item)
    {
        _queue.Enqueue(item);
        
        lock (_subscriberLock)
        {
            foreach (var subscriber in _subscribers)
            {
                if (!subscriber.IsAddingCompleted)
                {
                    subscriber.Add(item);
                }
            }
        }
    }

    public BlockingCollection<T> Subscribe()
    {
        var subscriberCollection = new BlockingCollection<T>();
        
        lock (_subscriberLock)
        {
            // Add existing queue items to new subscriber
            foreach (var item in _queue)
            {
                subscriberCollection.Add(item);
            }
            
            _subscribers.Add(subscriberCollection);
        }

        return subscriberCollection;
    }

    public void Unsubscribe(BlockingCollection<T> subscriber)
    {
        lock (_subscriberLock)
        {
            _subscribers.Remove(subscriber);
            subscriber.Dispose();
        }
    }

    public void CompleteAdding()
    {
        lock (_subscriberLock)
        {
            foreach (var subscriber in _subscribers)
            {
                subscriber.CompleteAdding();
            }
        }
    }
}