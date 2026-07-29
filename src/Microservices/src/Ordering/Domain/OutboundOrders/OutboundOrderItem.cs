using SharedKernel;

namespace Domain.OutboundOrders;

public sealed class OutboundOrderItem : Entity<int>
{
    public Guid ProductId { get; private set; }
    public int Quantity { get; private set; }

    private OutboundOrderItem()
    {
    }

    internal OutboundOrderItem(Guid productId, int quantity)
    {
        ProductId = productId;
        Quantity = quantity;
    }
}