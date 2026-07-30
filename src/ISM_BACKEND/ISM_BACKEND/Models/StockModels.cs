namespace ISM_BACKEND.Models;

public class StockItem
{
    public long productId { get; set; }
    public string? productNo { get; set; }
    public string? productName { get; set; }
    public string? unit { get; set; }
    public long warehouseId { get; set; }
    public string? warehouseName { get; set; }
    public int quantity { get; set; }
    public int cumulativeShipped { get; set; }
}
