using System.Reflection;

namespace NPubSub;

internal record RegisteredSubscription(MethodInfo RegistreeCallback, MethodInfo PubSubSubscribe, MethodInfo PubSubUnsubscribe, Type DelegateType, int Order);
internal record RegisteredPublication(EventInfo RegistreeEvent, MethodInfo PubSubPublish, Type PublishDelegate);

internal class Registration
{
    public Registration(List<RegisteredSubscription> registeredSubscriptions, List<RegisteredPublication> registeredPublication)
    {
        RegisteredSubscriptions = registeredSubscriptions;
        RegisteredPublication = registeredPublication;
    }

    public List<RegisteredSubscription> RegisteredSubscriptions { get; }
    public List<RegisteredPublication> RegisteredPublication { get; }
}
