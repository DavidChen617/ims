using ISM_BACKEND.Models;
using ISM_BACKEND.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ISM_BACKEND.Controllers;

[Route("api/inbound-orders")]
[Authorize]
public class InboundOrdersController : BaseController
{
    private readonly InboundOrderService _svc;

    public InboundOrdersController(InboundOrderService svc) => _svc = svc;

    public record ReasonBody(string reason);

    [HttpPost]
    [Authorize(Roles = "WarehouseUser")]
    public async Task<ActionResult<ApiResponse<object>>> Create([FromBody] CreateOrderRequest body)
    {
        var warehouseId = GetCurrentWarehouseId()!.Value;
        var orderId = await _svc.CreateInboundOrderAsync(warehouseId, GetRequiredUserId(), GetRequiredUserName(), body.orderNo, body.items);
        return Ok(ApiResponse<object>.Ok(new { orderId }, "入庫單建立成功"));
    }

    [HttpPost("{id:long}/confirm")]
    [Authorize(Roles = "WarehouseAdmin")]
    public async Task<ActionResult<ApiResponse<object>>> Confirm([FromRoute] long id)
        => await _svc.ConfirmInboundOrderAsync(id, GetRequiredUserId(), GetRequiredUserName())
            ? Ok(ApiResponse<object>.Ok(new { }, "入庫單已確認"))
            : BadRequest(ApiResponse<object>.Fail("僅能確認 Pending 狀態的入庫單"));

    [HttpPost("{id:long}/reject")]
    [Authorize(Roles = "WarehouseAdmin")]
    public async Task<ActionResult<ApiResponse<object>>> Reject([FromRoute] long id, [FromBody] ReasonBody body)
        => await _svc.RejectInboundOrderAsync(id, GetRequiredUserId(), GetRequiredUserName(), body.reason)
            ? Ok(ApiResponse<object>.Ok(new { }, "入庫單已拒絕"))
            : BadRequest(ApiResponse<object>.Fail("僅能拒絕 Pending 狀態的入庫單"));

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<OrderListItem>>>> List(
        [FromQuery] string? status,
        [FromQuery] string? orderNo,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        // Admin 可跨倉庫查看全部;其餘角色強制限定自己倉庫
        long? warehouseId = GetRequiredRole() == "Admin" ? null : GetCurrentWarehouseId();
        var data = await _svc.ListInboundOrdersAsync(warehouseId, status, orderNo, page, pageSize);
        return Ok(ApiResponse<PagedResult<OrderListItem>>.Ok(data));
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<ApiResponse<OrderDetail>>> Get([FromRoute] long id)
    {
        long? scope = GetRequiredRole() == "Admin" ? null : GetCurrentWarehouseId();
        var data = await _svc.GetInboundOrderAsync(id, scope);
        return data != null
            ? Ok(ApiResponse<OrderDetail>.Ok(data))
            : NotFound(ApiResponse<OrderDetail>.Fail("入庫單不存在"));
    }
}
