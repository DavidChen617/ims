using System.Text.Json;
using MessageContract.InboundOrders;

namespace Ordering.UnitTest;

public class IntegrationEventSerializationTests
{
    [Fact]
    public void GivenIntegrationEvent_WhenRoundTrippedThroughJson_ThenIdSurvives()
    {
        var original = new InboundOrderConfirmedIntegrationEvent(
            Guid.CreateVersion7(), Guid.CreateVersion7(), "warehouse-1", Guid.CreateVersion7(), []);

        var json = JsonSerializer.Serialize(original, original.GetType());
        var roundTripped = (InboundOrderConfirmedIntegrationEvent)JsonSerializer.Deserialize(
            json, original.GetType())!;

        Assert.Equal(original.Id, roundTripped.Id);
    }
}
