namespace Domain.InboundOrders;

public enum InboundOrderStatus
{
    Pending,
    Confirmed,
    Rejected
}

public static class InboundOrderStatusExtensions
{
    extension(InboundOrderStatus status)
    {
        public bool IsPending() => status == InboundOrderStatus.Pending;
    }
}
