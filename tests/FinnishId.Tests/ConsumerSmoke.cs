using FinnishId;

namespace ExternalConsumer.SmokeTests;

public class ConsumerSmokeTests
{
    [Fact]
    public void EntryClassResolvesFromConsumerNamespaceBeneathFinnishId()
    {
        Assert.True(HetuNumber.IsValid("131052-308T"));
        Assert.Equal(new DateOnly(1952, 10, 13), HetuNumber.Parse("131052-308T").DateOfBirth);
    }
}
