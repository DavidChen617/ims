namespace MessageContract;

public sealed record EnrichedOrderItem(
    Guid ProductId,
    string ProductNo,
    string ProductName,
    string Unit,
    int Quantity);
