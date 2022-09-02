using Nito.AsyncEx;

namespace NPubSub;

public class ConcurrentPubSub : IPubSub
{
    private readonly PubSub _pubSub = new();
    private readonly AsyncReaderWriterLock _readerWriterLock = new();

    public void Subscribe<TEvent>(SubscribeCallback<TEvent> subscribeCallback, int order = int.MaxValue)
    {
        using (_readerWriterLock.WriterLock())
        {
            _pubSub.Subscribe<TEvent>(subscribeCallback, order);
        }
    }
    
    public async Task SubscribeAsync<TEvent>(SubscribeCallback<TEvent> subscribeCallback, int order = int.MaxValue)
    {
        using (await _readerWriterLock.WriterLockAsync())
        {
            _pubSub.Subscribe(subscribeCallback, order);
        }
    }

    public bool Unsubscribe<TEvent>(SubscribeCallback<TEvent> subscribeCallback)
    {
        using (_readerWriterLock.WriterLock())
        {
            return _pubSub.Unsubscribe(subscribeCallback);
        }
    }
    
    public async Task<bool> UnsubscribeAsync<TEvent>(SubscribeCallback<TEvent> subscribeCallback)
    {
        using (await _readerWriterLock.WriterLockAsync())
        {
            return _pubSub.Unsubscribe(subscribeCallback);
        }
    }

    public Task PublishAsync<TEventArgs>(TEventArgs e)
    {
        using (_readerWriterLock.ReaderLock())
        {
            return _pubSub.PublishAsync(e);
        }
    }
}
