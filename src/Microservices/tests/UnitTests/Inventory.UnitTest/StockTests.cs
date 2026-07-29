using Domain.Stocks;

namespace Inventory.UnitTest;

public class StockTests
{
    private static readonly Guid ProductId = Guid.CreateVersion7();
    private static readonly Guid WarehouseId = Guid.CreateVersion7();

    private static Stock CreateStock() => Stock.Create(ProductId, WarehouseId).Value;

    [Fact]
    public void GivenValidInputs_WhenCreated_ThenPropertiesAreSetCorrectly()
    {
        var result = Stock.Create(ProductId, WarehouseId);

        Assert.True(result.IsSuccess);
        var stock = result.Value;
        Assert.NotEqual(Guid.Empty, stock.Id);
        Assert.Equal(ProductId, stock.ProductId);
        Assert.Equal(WarehouseId, stock.WarehouseId);
        Assert.Equal(0, stock.Quantity);
        Assert.Equal(0, stock.CumulativeShipped);
    }

    [Fact]
    public void GivenStock_WhenIncreased_ThenQuantityGoesUp()
    {
        var stock = CreateStock();

        stock.Increase(10);

        Assert.Equal(10, stock.Quantity);
    }

    [Fact]
    public void GivenStock_WhenDecreased_ThenQuantityGoesDown()
    {
        var stock = CreateStock();
        stock.Increase(10);

        stock.Decrease(4);

        Assert.Equal(6, stock.Quantity);
    }

    [Fact]
    public void GivenSufficientStock_WhenReserved_ThenSucceedsAndUpdatesQuantityAndCumulativeShipped()
    {
        var stock = CreateStock();
        stock.Increase(10);

        var result = stock.TryReserve(4);

        Assert.True(result.IsSuccess);
        Assert.Equal(6, stock.Quantity);
        Assert.Equal(4, stock.CumulativeShipped);
    }

    [Fact]
    public void GivenInsufficientStock_WhenReserved_ThenFailsAndLeavesStateUnchanged()
    {
        var stock = CreateStock();
        stock.Increase(3);

        var result = stock.TryReserve(4);

        Assert.False(result.IsSuccess);
        Assert.Equal(3, stock.Quantity);
        Assert.Equal(0, stock.CumulativeShipped);
    }

    [Fact]
    public void GivenReservedStock_WhenReservationReleased_ThenQuantityAndCumulativeShippedAreReversed()
    {
        var stock = CreateStock();
        stock.Increase(10);
        stock.TryReserve(4);

        stock.ReleaseReservation(4);

        Assert.Equal(10, stock.Quantity);
        Assert.Equal(0, stock.CumulativeShipped);
    }
}
