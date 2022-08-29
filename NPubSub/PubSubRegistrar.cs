using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace NPubSub;

public class PubSubRegistrar
{
    // "!" because obviously these calls will always return a method
    private static readonly MethodInfo _eventBusSubscribe = typeof(ConcurrentPubSub).GetMethod(nameof(ConcurrentPubSub.Subscribe))!;
    private static readonly MethodInfo _eventBusUnsubscribe = typeof(ConcurrentPubSub).GetMethod(nameof(ConcurrentPubSub.Unsubscribe))!;
    private static readonly MethodInfo _eventBusPublish = typeof(ConcurrentPubSub).GetMethod(nameof(ConcurrentPubSub.PublishAsync))!;
    private static readonly Type _genericSubscriptionDelegateType = typeof(SubscribeCallback<>);

    private readonly IPubSub _pubSub;
    private readonly Dictionary<Type, Registration> registrations = new();

    public PubSubRegistrar(IPubSub pubSub)
    {
        _pubSub = pubSub;
    }

    private bool TryGetRegistration(Type type, [MaybeNullWhen(false)] out Registration result)
    {
        return registrations.TryGetValue(type, out result);
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
            Type specializedDelegateType = _genericSubscriptionDelegateType.MakeGenericType(eventType);
            MethodInfo eventBusSubscribeSpecialized = _eventBusSubscribe.MakeGenericMethod(eventType);
            MethodInfo eventBusUnsubscribeSpecialized = _eventBusUnsubscribe.MakeGenericMethod(eventType);

            registeredSubscriptions.Add(new RegisteredSubscription(methodInfo, eventBusSubscribeSpecialized, eventBusUnsubscribeSpecialized, specializedDelegateType, subscriberAttribute.Order));
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

                MethodInfo eventBusPublish = _eventBusPublish.MakeGenericMethod(eventType);

                registeredPublications.Add(new RegisteredPublication(eventInfo, eventBusPublish, specializedDelegateType));
            }
        }

        Registration registration = new Registration(registeredSubscriptions, registeredPublications);
        registrations.Add(type, registration);

        return registration;
    }

    private void SubscribeRegistreeToEventBus(List<RegisteredSubscription> registeredSubscriptions, IPubSub pubSub, object registeree)
    {
        foreach (RegisteredSubscription registeredSubscription in registeredSubscriptions)
        {
            var delegateInstance = registeredSubscription.RegistreeCallback.CreateDelegate(registeredSubscription.DelegateType, registeree);
            int order = registeredSubscription.Order;
            object[] arguments = new object[] { delegateInstance, order };
            registeredSubscription.PubSubSubscribe.Invoke(pubSub, arguments);
        }
    }

    private void SubscribeEventBusToRegistree(List<RegisteredPublication> registeredPublications, IPubSub pubSub, object registeree)
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

        SubscribeRegistreeToEventBus(registration.RegisteredSubscriptions, _pubSub, registree);
        SubscribeEventBusToRegistree(registration.RegisteredPublication, _pubSub, registree);
    }

    public void Unregister(object registree)
    {
        Type type = registree.GetType();
        Registration registration = CreateRegistration(type);

        UnsubscribeRegistreeFromEventBus(registration.RegisteredSubscriptions, _pubSub, registree);
        UnsubscribeEventBusFromRegistree(registration.RegisteredPublication, _pubSub, registree);
    }

    private void UnsubscribeRegistreeFromEventBus(List<RegisteredSubscription> registeredSubscriptions, IPubSub pubSub, object registeree)
    {
        foreach (RegisteredSubscription registeredSubscription in registeredSubscriptions)
        {
            var delegateInstance = registeredSubscription.RegistreeCallback.CreateDelegate(registeredSubscription.DelegateType, registeree);
            object[] arguments = { delegateInstance };
            registeredSubscription.PubSubUnsubscribe.Invoke(pubSub, arguments);
        }
    }

    private void UnsubscribeEventBusFromRegistree(List<RegisteredPublication> registeredPublications, IPubSub pubSub, object registeree)
    {
        foreach (RegisteredPublication registeredPublication in registeredPublications)
        {
            Delegate publishDelegate = registeredPublication.PubSubPublish.CreateDelegate(registeredPublication.PublishDelegate, pubSub);
            registeredPublication.RegistreeEvent.RemoveEventHandler(registeree, publishDelegate);
        }
    }
}