namespace NPubSub;

public delegate Task SubscribeCallback<in TEvent>(TEvent e);

public interface IPubSub
{
    void Subscribe<TEvent>(SubscribeCallback<TEvent> subscribeCallback, int order = int.MaxValue);
    bool Unsubscribe<TEvent>(SubscribeCallback<TEvent> subscribeCallback);
    Task PublishAsync<TEventArgs>(TEventArgs e);
}