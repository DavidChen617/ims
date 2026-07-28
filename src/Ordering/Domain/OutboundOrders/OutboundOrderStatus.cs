namespace Domain.OutboundOrders;

public enum OutboundOrderStatus
{
    Processing,
    Pending,
    Confirmed,
    Rejected
}

public static class OutboundOrderStatusExtensions
{
    extension(OutboundOrderStatus status)
    {
        public bool IsProcessing() => status == OutboundOrderStatus.Processing;
        public bool IsPending() => status == OutboundOrderStatus.Pending;
    }
}
