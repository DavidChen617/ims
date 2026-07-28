namespace Infrastructure.Messaging;

public sealed class KafkaTopicOptions
{
    // 消費端 —— Ordering 自己發出的訊息流。
    public string Ordering { get; set; } = string.Empty;

    // 給那些這個服務的 consumer 處理不了、來自 Ordering 的訊息用的 dead-letter。
    public string OrderingDeadLetter { get; set; } = string.Empty;

    // 發布端 —— 這個服務自己發出的訊息流(Ordering 會消費回去)。
    public string Inventory { get; set; } = string.Empty;
}
