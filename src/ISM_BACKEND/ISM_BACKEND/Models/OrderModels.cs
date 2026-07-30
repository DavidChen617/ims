namespace ISM_BACKEND.Models;

public class CreateOrderItemRequest
{
    public long productId { get; set; }
    public string? productNo { get; set; }
    public int quantity { get; set; }
    public decimal? unitPrice { get; set; } // 只有 InboundOrder 使用，未帶則取商品當下 Price
}

public class CreateOrderRequest
{
    public string orderNo { get; set; } = "";
    public List<CreateOrderItemRequest> items { get; set; } = new();
}

public class OrderItemDto
{
    public long productId { get; set; }
    public string productNo { get; set; } = "";
    public string productName { get; set; } = "";
    public string unit { get; set; } = "";
    public int quantity { get; set; }
    public decimal? unitPrice { get; set; }
}

public class OrderListItem
{
    public long orderId { get; set; }
    public string orderNo { get; set; } = "";
    public long warehouseId { get; set; }
    public string status { get; set; } = "";
    public DateTime requestedAt { get; set; }
    public DateTime? confirmedAt { get; set; }
}

public class OrderDetail
{
    public long orderId { get; set; }
    public string orderNo { get; set; } = "";
    public long warehouseId { get; set; }
    public string status { get; set; } = "";
    public string? rejectReason { get; set; }
    public long requestedBy { get; set; }
    public string requestedByName { get; set; } = "";
    public DateTime requestedAt { get; set; }
    public long? confirmedBy { get; set; }
    public string? confirmedByName { get; set; }
    public DateTime? confirmedAt { get; set; }
    public List<OrderItemDto> items { get; set; } = new();
}
