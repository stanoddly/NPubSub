using NPubSub.Concurrent;
using NUnit.Framework;

namespace NPubSub.Tests;

[TestFixture]
public class ConcurrentPubSubTests: PubSubTestsBase
{
    [SetUp]
    public void SetUp()
    {
        _pubSub = new ConcurrentPubSub();
    }
}
