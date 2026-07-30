using ISM_BACKEND.Models;
using ISM_BACKEND.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ISM_BACKEND.Controllers;

[Route("api/warehouses")]
[Authorize(Roles = "Admin")]
public class WarehousesController : BaseController
{
    private readonly WarehouseService _svc;

    public WarehousesController(WarehouseService svc) => _svc = svc;

    [HttpPost]
    public async Task<ActionResult<ApiResponse<object>>> Create([FromBody] CreateWarehouseRequest body)
    {
        var id = await _svc.CreateWarehouseAsync(body.name);
        return Ok(ApiResponse<object>.Ok(new { warehouseId = id }, "倉庫建立成功"));
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<WarehouseListItem>>>> List(
        [FromQuery] string? name, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var data = await _svc.ListWarehousesAsync(name, page, pageSize);
        return Ok(ApiResponse<PagedResult<WarehouseListItem>>.Ok(data));
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<ApiResponse<WarehouseDetail>>> Get([FromRoute] long id)
    {
        var data = await _svc.GetWarehouseDetailAsync(id);
        return data != null
            ? Ok(ApiResponse<WarehouseDetail>.Ok(data))
            : NotFound(ApiResponse<WarehouseDetail>.Fail("倉庫不存在"));
    }
}
