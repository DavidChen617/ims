using ISM_BACKEND.Models;
using ISM_BACKEND.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ISM_BACKEND.Controllers;

[Route("api/stocks")]
[Authorize]
public class StocksController : BaseController
{
    private readonly StockService _svc;

    public StocksController(StockService svc) => _svc = svc;

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<PagedResult<StockItem>>>> List(
        [FromQuery] long? warehouseId, [FromQuery] long? productId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var data = await _svc.ListStocksAsync(warehouseId, productId, page, pageSize);
        return Ok(ApiResponse<PagedResult<StockItem>>.Ok(data));
    }

    [HttpGet("warehouse")]
    [Authorize(Roles = "WarehouseAdmin,WarehouseUser")]
    public async Task<ActionResult<ApiResponse<PagedResult<StockItem>>>> ListForWarehouse(
        [FromQuery] long? productId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var data = await _svc.ListStocksAsync(GetCurrentWarehouseId(), productId, page, pageSize);
        return Ok(ApiResponse<PagedResult<StockItem>>.Ok(data));
    }
}
