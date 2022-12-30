using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using NPubSub.ReflectionRegistration;

namespace NPubSub;

public class PubSubRegistrar
{
    // "!" because obviously these calls will always return a method
    private static readonly MethodInfo PubSubSubscribe = typeof(IPubSub).GetMethod(nameof(IPubSub.Subscribe))!;
    private static readonly MethodInfo PubSubUnsubscribe = typeof(IPubSub).GetMethod(nameof(IPubSub.Unsubscribe))!;
    private static readonly MethodInfo PubSubPublishAsync = typeof(IPubSub).GetMethod(nameof(IPubSub.PublishAsync))!;
    private static readonly Type GenericSubscriptionDelegateType = typeof(SubscribeCallback<>);

    private readonly IPubSub _pubSub;
    private readonly Dictionary<Type, Registration> _registrations = new();

    public PubSubRegistrar(IPubSub pubSub)
    {
        _pubSub = pubSub;
    }

    private bool TryGetRegistration(Type type, [MaybeNullWhen(false)] out Registration result)
    {
        return _registrations.TryGetValue(type, out result);
    }

    private Registration CreateRegistration(Type type)
    {
        if (TryGetRegistration(type, out Registration? result))
        {
            return result;
        }

        List<RegisteredSubscription> registeredSubscriptions = new();

        foreach (MethodInfo methodInfo in type.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
        {
            SubscriberAttribute? subscriberAttribute = methodInfo.GetCustomAttribute(typeof(SubscriberAttribute)) as SubscriberAttribute;

            if (subscriberAttribute == null)
            {
                continue;
            }

            var parameters = methodInfo.GetParameters();

            if (parameters.Length != 1)
            {
                throw new ArgumentException("Expected 1 argument.");
            }
                     
            ParameterInfo firstParameterInfo = parameters[0];
            Type eventType = firstParameterInfo.ParameterType;
            // create a delegate from SubscriptionDelegate with event type
            Type specializedDelegateType = GenericSubscriptionDelegateType.MakeGenericType(eventType);
            MethodInfo pubSubSubscribeSpecialized = PubSubSubscribe.MakeGenericMethod(eventType);
            MethodInfo pubSubUnsubscribeSpecialized = PubSubUnsubscribe.MakeGenericMethod(eventType);

            registeredSubscriptions.Add(new RegisteredSubscription(methodInfo, pubSubSubscribeSpecialized, pubSubUnsubscribeSpecialized, specializedDelegateType, subscriberAttribute.Order));
        }

        List<RegisteredPublication> registeredPublications = new();

        foreach (EventInfo eventInfo in type.GetEvents(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
        {
            bool isPublication = eventInfo.IsDefined(typeof(PublisherAttribute), true);

            if (isPublication)
            {
                MethodInfo invoke = eventInfo.EventHandlerType!.GetMethod("Invoke")!;
                ParameterInfo[] parameters = invoke.GetParameters();

                if (parameters.Length != 1)
                {
                    throw new ArgumentException("Expected 1 argument.");
                }

                Type eventType = parameters[0].ParameterType;

                Type publicationDelegate = typeof(PublishHandler<>);
                Type specializedDelegateType = publicationDelegate.MakeGenericType(eventType);

                MethodInfo pubSubPublish = PubSubPublishAsync.MakeGenericMethod(eventType);

                registeredPublications.Add(new RegisteredPublication(eventInfo, pubSubPublish, specializedDelegateType));
            }
        }

        Registration registration = new Registration(registeredSubscriptions, registeredPublications);
        _registrations.Add(type, registration);

        return registration;
    }

    private void SubscribeRegistreeToPubSub(List<RegisteredSubscription> registeredSubscriptions, IPubSub pubSub, object registeree)
    {
        foreach (RegisteredSubscription registeredSubscription in registeredSubscriptions)
        {
            var delegateInstance = registeredSubscription.RegistreeCallback.CreateDelegate(registeredSubscription.DelegateType, registeree);
            int order = registeredSubscription.Order;
            object[] arguments = new object[] { delegateInstance, order };
            registeredSubscription.PubSubSubscribe.Invoke(pubSub, arguments);
        }
    }

    private void SubscribePubSubToRegistree(List<RegisteredPublication> registeredPublications, IPubSub pubSub, object registeree)
    {
        foreach (RegisteredPublication registeredPublication in registeredPublications)
        {
            Delegate publishDelegate = registeredPublication.PubSubPublish.CreateDelegate(registeredPublication.PublishDelegate, pubSub);
            registeredPublication.RegistreeEvent.AddEventHandler(registeree, publishDelegate);
        }
    }

    public void Register(object registree)
    {
        Type type = registree.GetType();
        Registration registration = CreateRegistration(type);

        SubscribeRegistreeToPubSub(registration.RegisteredSubscriptions, _pubSub, registree);
        SubscribePubSubToRegistree(registration.RegisteredPublication, _pubSub, registree);
    }

    public void Unregister(object registree)
    {
        Type type = registree.GetType();
        Registration registration = CreateRegistration(type);

        UnsubscribeRegistreeFromPubSub(registration.RegisteredSubscriptions, _pubSub, registree);
        UnsubscribePubSubFromRegistree(registration.RegisteredPublication, _pubSub, registree);
    }

    private void UnsubscribeRegistreeFromPubSub(List<RegisteredSubscription> registeredSubscriptions, IPubSub pubSub, object registeree)
    {
        foreach (RegisteredSubscription registeredSubscription in registeredSubscriptions)
        {
            var delegateInstance = registeredSubscription.RegistreeCallback.CreateDelegate(registeredSubscription.DelegateType, registeree);
            object[] arguments = { delegateInstance };
            registeredSubscription.PubSubUnsubscribe.Invoke(pubSub, arguments);
        }
    }

    private void UnsubscribePubSubFromRegistree(List<RegisteredPublication> registeredPublications, IPubSub pubSub, object registeree)
    {
        foreach (RegisteredPublication registeredPublication in registeredPublications)
        {
            Delegate publishDelegate = registeredPublication.PubSubPublish.CreateDelegate(registeredPublication.PublishDelegate, pubSub);
            registeredPublication.RegistreeEvent.RemoveEventHandler(registeree, publishDelegate);
        }
    }
}
