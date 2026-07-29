namespace Infrastructure.Messaging;

public sealed class IntegrationEventSubscriptionInfo
{
    public Dictionary<string, Type> EventTypes { get; } = [];
}
