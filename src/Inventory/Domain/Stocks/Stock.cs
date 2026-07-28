using Davish.Result;
using SharedKernel;

namespace Domain.Stocks;

public sealed class Stock : AggregateRoot
{
    public Guid ProductId { get; private set; }
    public Guid WarehouseId { get; private set; }
    public int Quantity { get; private set; }
    public int CumulativeShipped { get; private set; }

    public string? ProductNo { get; private set; }
    public string? ProductName { get; private set; }
    public string? Unit { get; private set; }
    public string? WarehouseName { get; private set; }

    public static Result<Stock> Create(Guid productId, Guid warehouseId)
    {
        return new Stock
        {
            Id = Guid.CreateVersion7(),
            ProductId = productId,
            WarehouseId = warehouseId
        };
    }

    public void Increase(int quantity)
    {
        Quantity += quantity;
    }

    public void SetDisplayInfo(string productNo, string productName, string unit, string? warehouseName)
    {
        ProductNo = productNo;
        ProductName = productName;
        Unit = unit;
        WarehouseName = warehouseName;
    }

    // 如果同一筆資料在原始的 Increase 跟這次的還原之間,被另一個併發的 TryReserve 扣走了庫存,
    // 這裡是有可能合理地變成負數的 —— 已知情況,目前刻意不加防護。
    public void Decrease(int quantity)
    {
        Quantity -= quantity;
    }

    public Result TryReserve(int quantity)
    {
        if (Quantity < quantity)
            return new Error("Stock.TryReserve", "Insufficient stock", ErrorType.Conflict);

        Quantity -= quantity;
        CumulativeShipped += quantity;

        return Result.Success();
    }

    // 跟 Decrease 一樣的告誡:在不好的交錯情況下,CumulativeShipped 有可能變成負數。
    // 目前刻意不加防護。
    public void ReleaseReservation(int quantity)
    {
        Quantity += quantity;
        CumulativeShipped -= quantity;
    }
}
