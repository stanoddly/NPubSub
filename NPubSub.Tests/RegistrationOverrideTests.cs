using System.Threading.Tasks;
using NPubSub.ReflectionRegistration;
using NUnit.Framework;

namespace NPubSub.Tests
{
    class TestSubscriberBase
    {
        public int CallCount { get; set; }
        public bool BaseHandled { get; set; }

        [Subscriber]
        public virtual Task Handle(TestEvent testEvent)
        {
            CallCount++;
            BaseHandled = true;
            return Task.CompletedTask;
        }
    }

    class TestSubscriberBaseDerived : TestSubscriberBase
    {
        public bool DerivedHandled { get; set; }

        [Subscriber]
        public override Task Handle(TestEvent testEvent)
        {
            CallCount++;
            DerivedHandled = true;
            return Task.CompletedTask;
        }
    }

    public class RegistrationOverrideTests
    {
        [Test]
        public async Task TestOverridenSubscriptionIsSubscribed()
        {
            ConcurrentPubSub concurrentPubSub = new();
            PubSubRegistrar pubSubRegistrar = new(concurrentPubSub);
            
            TestSubscriberBaseDerived subscriber = new();

            pubSubRegistrar.Register(subscriber);

            await concurrentPubSub.PublishAsync(new TestEvent(42));

            Assert.AreEqual(subscriber.CallCount, 1);
            Assert.AreEqual(subscriber.BaseHandled, false);
            Assert.AreEqual(subscriber.DerivedHandled, true);
        }
    }
}

