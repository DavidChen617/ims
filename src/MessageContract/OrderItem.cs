namespace MessageContract;

// 用具名的 record,不是 ValueTuple —— System.Text.Json 預設只會序列化 property
// (ValueTuple 的 Item1/Item2 是 field,所以會悄悄地序列化成 `{}`)。
public sealed record OrderItem(Guid ProductId, int Quantity);
