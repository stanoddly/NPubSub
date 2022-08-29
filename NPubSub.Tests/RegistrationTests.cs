using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;

namespace NPubSub.Tests
{
    class TestPublisher
    {
        [Publisher]
        public event PublishHandler<TestEvent>? TestEventHandler;

        public Task OnTestEventAsync(TestEvent testEvent)
        {
            return TestEventHandler?.Invoke(testEvent) ?? Task.CompletedTask;
        }
    }

    class TestSubscriber
    {
        public TestEvent? LastHandledTestEvent { get; set; }
        public int CallCount { get; set; }
        public List<TestEvent> CapturedEvents = new();

        [Subscriber]
        public Task Handle(TestEvent testEvent)
        {
            CallCount++;
            LastHandledTestEvent = testEvent;
            CapturedEvents.Add(testEvent);
            return Task.CompletedTask;
        }
    }

    class TestSubscriberOrdered
    {
        public List<int> CapturedOrders = new();

        [Subscriber(3)]
        public Task Handle1(TestEvent _)
        {
            CapturedOrders.Add(3);
            return Task.CompletedTask;
        }

        [Subscriber(1)]
        public Task Handle2(TestEvent _)
        {
            CapturedOrders.Add(1);
            return Task.CompletedTask;
        }

        [Subscriber(2)]
        public Task Handle3(TestEvent _)
        {
            CapturedOrders.Add(2);
            return Task.CompletedTask;
        }
    }

    public class RegistrationTests
    {
        private IPubSub _pubSub = null!;
        private PubSubRegistrar _pubSubRegistrar = null!;

        [SetUp]
        protected void SetUp()
        {
            _pubSub = new ConcurrentPubSub();
            _pubSubRegistrar = new PubSubRegistrar(_pubSub);
        }
        
        [Test]
        public async Task TestRegistration()
        {
            TestSubscriber testSubscriber = new();
            TestPublisher testPublisher = new();

            _pubSubRegistrar.Register(testSubscriber);
            _pubSubRegistrar.Register(testPublisher);

            TestEvent testEvent = new(42);
            await testPublisher.OnTestEventAsync(testEvent);

            Assert.AreEqual(testEvent, testSubscriber.LastHandledTestEvent);
            Assert.AreEqual(1, testSubscriber.CallCount);
        }

        [Test]
        public async Task TestOrderedSubscriptions()
        {
            TestSubscriberOrdered testSubscriberOrdered = new();

            _pubSubRegistrar.Register(testSubscriberOrdered);

            await _pubSub.PublishAsync(new TestEvent(42));

            CollectionAssert.AreEqual(testSubscriberOrdered.CapturedOrders, new List<int> { 1, 2, 3 });
        }

        [Test]
        public async Task TestUnregistrerSubscriber()
        {
            TestSubscriber testSubscriber1 = new();
            TestSubscriber testSubscriber2 = new();
            TestPublisher testPublisher = new();

            _pubSubRegistrar.Register(testSubscriber1);
            _pubSubRegistrar.Register(testSubscriber2);
            _pubSubRegistrar.Register(testPublisher);

            TestEvent testEvent1 = new(42);
            await testPublisher.OnTestEventAsync(testEvent1);

            _pubSubRegistrar.Unregister(testSubscriber2);

            TestEvent testEvent2 = new(43);
            await testPublisher.OnTestEventAsync(testEvent2);

            Assert.AreEqual(testEvent2, testSubscriber1.LastHandledTestEvent);
            Assert.AreEqual(2, testSubscriber1.CallCount);

            Assert.AreEqual(testEvent1, testSubscriber2.LastHandledTestEvent);
            Assert.AreEqual(1, testSubscriber2.CallCount);
        }

        [Test]
        public async Task TestUnregistrationPublish()
        {
            TestSubscriber testSubscriber = new();
            TestPublisher testPublisher1 = new();
            TestPublisher testPublisher2 = new();

            _pubSubRegistrar.Register(testSubscriber);
            _pubSubRegistrar.Register(testPublisher1);
            _pubSubRegistrar.Register(testPublisher2);

            TestEvent testEvent1 = new(42);
            await testPublisher1.OnTestEventAsync(testEvent1);

            TestEvent testEvent2 = new(43);
            await testPublisher2.OnTestEventAsync(testEvent2);

            _pubSubRegistrar.Unregister(testPublisher1);

            TestEvent testEvent3 = new(44);
            await testPublisher1.OnTestEventAsync(testEvent3);

            TestEvent testEvent4 = new(45);
            await testPublisher2.OnTestEventAsync(testEvent4);
            
            Assert.AreEqual(3, testSubscriber.CallCount);
            Assert.AreEqual(testEvent4, testSubscriber.LastHandledTestEvent);
            CollectionAssert.AreEqual(new[] { testEvent1, testEvent2, testEvent4 }, testSubscriber.CapturedEvents);
        }
    }
}

