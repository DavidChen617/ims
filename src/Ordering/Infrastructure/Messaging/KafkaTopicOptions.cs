namespace Infrastructure.Messaging;

public sealed class KafkaTopicOptions
{
    public string Ordering { get; set; } = string.Empty;
    public string OrderingDeadLetter { get; set; } = string.Empty;
    public string Inventory { get; set; } = string.Empty;
    public string InventoryDeadLetter { get; set; } = string.Empty;
}
