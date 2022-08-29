using System.Threading.Tasks;
using NUnit.Framework;

namespace NPubSub.Tests
{
    internal class TestClass
    {
        public TestEvent? LastHandledTestEvent { get; set; }
        public int CallCount { get; set; }

        public Task Handle(TestEvent testEvent)
        {
            CallCount++;
            LastHandledTestEvent = testEvent;
            return Task.CompletedTask;
        }
    }

    [TestFixture]
    public abstract class PubSubTestsBase
    {
        protected IPubSub _pubSub = null!;
        
        [Test]
        public async Task TestPublishOneInvokesOneSubscribed()
        {
            TestClass testClass = new();

            _pubSub.Subscribe<TestEvent>(testClass.Handle);

            TestEvent testEvent = new(42);
            await _pubSub.PublishAsync(testEvent);

            Assert.AreEqual(testEvent, testClass.LastHandledTestEvent);
            Assert.AreEqual(1, testClass.CallCount);
        }

        [Test]
        public async Task TestPublishOneEventInvokesMultipleSubscribers()
        {
            TestClass testClass1 = new();
            TestClass testClass2 = new();

            _pubSub.Subscribe<TestEvent>(testClass1.Handle);
            _pubSub.Subscribe<TestEvent>(testClass2.Handle);

            TestEvent testEvent = new(42); 
            await _pubSub.PublishAsync(testEvent);

            Assert.AreEqual(testEvent, testClass1.LastHandledTestEvent);
            Assert.AreEqual(1, testClass1.CallCount);
            Assert.AreEqual(testEvent, testClass2.LastHandledTestEvent);
            Assert.AreEqual(1, testClass2.CallCount);
        }

        [Test]
        public async Task TestUnsubscribeInstanceMethodIsSupported()
        {
            TestClass testClass = new();

            _pubSub.Subscribe<TestEvent>(testClass.Handle);

            bool unsubscribed = _pubSub.Unsubscribe<TestEvent>(testClass.Handle);
            TestEvent testEvent = new(42);
            await _pubSub.PublishAsync(testEvent);

            Assert.IsTrue(unsubscribed);
            Assert.AreEqual(null, testClass.LastHandledTestEvent);
            Assert.AreEqual(0, testClass.CallCount);
        }

        [Test]
        public async Task TestUnsubscribeNonexistent()
        {
            TestClass testClass = new();

            bool unsubscribed = _pubSub.Unsubscribe<TestEvent>(testClass.Handle);
            TestEvent testEvent = new(42);
            await _pubSub.PublishAsync(testEvent);

            Assert.IsFalse(unsubscribed);
            Assert.AreEqual(null, testClass.LastHandledTestEvent);
            Assert.AreEqual(0, testClass.CallCount);
        }
    }
}
