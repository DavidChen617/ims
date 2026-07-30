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

    // 尚未持久化的異動量。Repository 用這個對資料庫下「相對值」的原子更新(quantity = quantity + delta),
    // 不直接把算出來的絕對值蓋過去,藉此避免併發寫入互相覆蓋掉對方的異動(lost update)。
    public int QuantityDelta { get; private set; }
    public int CumulativeShippedDelta { get; private set; }

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
        QuantityDelta += quantity;
    }

    public void SetDisplayInfo(string productNo, string productName, string unit, string? warehouseName)
    {
        ProductNo = productNo;
        ProductName = productName;
        Unit = unit;
        WarehouseName = warehouseName;
    }

    public void Decrease(int quantity)
    {
        Quantity -= quantity;
        QuantityDelta -= quantity;
    }

    public Result TryReserve(int quantity)
    {
        if (Quantity < quantity)
            return new Error("Stock.TryReserve", "Insufficient stock", ErrorType.Conflict);

        Quantity -= quantity;
        CumulativeShipped += quantity;
        QuantityDelta -= quantity;
        CumulativeShippedDelta += quantity;

        return Result.Success();
    }

    public void ReleaseReservation(int quantity)
    {
        Quantity += quantity;
        CumulativeShipped -= quantity;
        QuantityDelta += quantity;
        CumulativeShippedDelta -= quantity;
    }

    // Repository 在一筆異動成功寫入資料庫後呼叫,把已經反映到 DB 的異動量歸零,
    // 避免同一個物件之後再次被存檔時,把已經寫過的量重複疊加上去。
    public void ClearPendingChanges()
    {
        QuantityDelta = 0;
        CumulativeShippedDelta = 0;
    }
}
