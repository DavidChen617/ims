namespace ISM_BACKEND.StateMachines;

public enum OrderStatus
{
    Unknown = 0,
    Pending = 1,
    Confirmed = 2,
    Rejected = 3
}

public static class OrderStateMachine
{
    private static readonly Dictionary<OrderStatus, string> Map = new()
    {
        [OrderStatus.Pending] = "Pending",
        [OrderStatus.Confirmed] = "Confirmed",
        [OrderStatus.Rejected] = "Rejected"
    };

    public static string ToApiString(OrderStatus status)
        => Map.TryGetValue(status, out var s) ? s : "Unknown";

    public static OrderStatus FromApiString(string? status)
        => Map.FirstOrDefault(kv => kv.Value.Equals(status, StringComparison.OrdinalIgnoreCase)).Key;
}
