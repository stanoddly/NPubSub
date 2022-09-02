using NUnit.Framework;

namespace NPubSub.Tests;

[TestFixture]
public class PubSubTests: PubSubTestsBase
{
    [SetUp]
    public void SetUp()
    {
        _pubSub = new PubSub();
    }
}
