using SharedKernel;

namespace Domain.InboundOrders;

public sealed class InboundOrderItem : Entity<int>
{
    public Guid ProductId { get; private set; }
    public int Quantity { get; private set; }
    public UnitPrice UnitPrice { get; private set; } = null!;

    private InboundOrderItem()
    {
    }

    internal InboundOrderItem(Guid productId, int quantity, UnitPrice unitPrice)
    {
        ProductId = productId;
        Quantity = quantity;
        UnitPrice = unitPrice;
    }
}