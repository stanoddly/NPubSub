using System.Collections;

namespace NPubSub;

public class PubSub: IPubSub
{
    private record SubscribeCallbackItem<TEventArgs>(SubscribeCallback<TEventArgs> Callback, int Order);
    
    private readonly Dictionary<Type, IList> _subscriptionsPerType = new();
    public void Subscribe<TEvent>(SubscribeCallback<TEvent> subscribeCallback, int order = Int32.MaxValue)
    {
        Type eventType = typeof(TEvent);
        if (_subscriptionsPerType.TryGetValue(eventType, out IList? uncastedSubscription))
        {
            List<SubscribeCallbackItem<TEvent>> subs = (List<SubscribeCallbackItem<TEvent>>)uncastedSubscription;
            subs.Add(new SubscribeCallbackItem<TEvent>(subscribeCallback, order));
            subs.Sort((item1, item2) => item1.Order.CompareTo(item2.Order));
        }
        else
        {
            _subscriptionsPerType[eventType] = new List<SubscribeCallbackItem<TEvent>>
                { new SubscribeCallbackItem<TEvent>(subscribeCallback, order) };
        }
    }

    public bool Unsubscribe<TEvent>(SubscribeCallback<TEvent> subscribeCallback)
    {
        Type eventType = typeof(TEvent);

        if (_subscriptionsPerType.TryGetValue(eventType, out IList? uncastedSubscription))
        {
            List<SubscribeCallbackItem<TEvent>> subs = (List<SubscribeCallbackItem<TEvent>>)uncastedSubscription;
            int removedCount = subs.RemoveAll(item => item.Callback.Equals(subscribeCallback));
            return removedCount > 0;
        }

        return false;
    }

    public Task PublishAsync<TEventArgs>(TEventArgs e)
    {
        if (_subscriptionsPerType.TryGetValue(typeof(TEventArgs), out IList? uncastedSubscription))
        {
            List<SubscribeCallbackItem<TEventArgs>> subscriptions =
                (List<SubscribeCallbackItem<TEventArgs>>)uncastedSubscription;

            List<Task> tasks = subscriptions.Select(subscription => subscription.Callback(e)).ToList();

            return Task.WhenAll(tasks);
        }
        return Task.CompletedTask;
    }
}