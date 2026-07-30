using ISM_BACKEND.Models;
using ISM_BACKEND.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ISM_BACKEND.Controllers;

[Route("api/products")]
[Authorize]
public class ProductsController : BaseController
{
    private readonly ProductService _svc;

    public ProductsController(ProductService svc) => _svc = svc;

    [HttpPost("units")]
    [Authorize(Roles = "Admin,WarehouseAdmin")]
    public async Task<ActionResult<ApiResponse<object>>> CreateUnit([FromBody] CreateProductUnitRequest body)
    {
        await _svc.CreateProductUnitAsync(body.name);
        return Ok(ApiResponse<object>.Ok(new { }, "商品單位建立成功"));
    }

    [HttpDelete("units/{name}")]
    [Authorize(Roles = "Admin,WarehouseAdmin")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteUnit([FromRoute] string name)
        => await _svc.DeleteProductUnitAsync(name)
            ? Ok(ApiResponse<object>.Ok(new { }, "商品單位已刪除"))
            : NotFound(ApiResponse<object>.Fail("商品單位不存在"));

    [HttpGet("units")]
    public async Task<ActionResult<ApiResponse<List<ProductUnitItem>>>> ListUnits()
    {
        var data = await _svc.ListProductUnitsAsync();
        return Ok(ApiResponse<List<ProductUnitItem>>.Ok(data));
    }

    [HttpPost]
    [Authorize(Roles = "Admin,WarehouseAdmin")]
    public async Task<ActionResult<ApiResponse<object>>> Create([FromBody] CreateProductRequest body)
    {
        var id = await _svc.CreateProductAsync(body.productNo, body.name, body.unit, body.price);
        return Ok(ApiResponse<object>.Ok(new { productId = id }, "商品建立成功"));
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<ProductItem>>>> List(
        [FromQuery] string? productNo,
        [FromQuery] string? name,
        [FromQuery] string? unit,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var data = await _svc.ListProductsAsync(productNo, name, unit, page, pageSize);
        return Ok(ApiResponse<PagedResult<ProductItem>>.Ok(data));
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<ApiResponse<ProductItem>>> Get([FromRoute] long id)
    {
        var data = await _svc.GetProductAsync(id);
        return data != null
            ? Ok(ApiResponse<ProductItem>.Ok(data))
            : NotFound(ApiResponse<ProductItem>.Fail("商品不存在"));
    }
}
