namespace NPubSub.ReflectionRegistration;

[AttributeUsage(AttributeTargets.Method)]
public class SubscriberAttribute : Attribute
{
    public int Order { get; } = int.MaxValue;
    public SubscriberAttribute() { }

    public SubscriberAttribute(int order)
    {
        Order = order;
    }
}

[AttributeUsage(AttributeTargets.Event | AttributeTargets.Field)]
public class PublisherAttribute : Attribute { }
