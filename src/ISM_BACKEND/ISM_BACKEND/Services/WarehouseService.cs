using ISM_BACKEND.Base;
using ISM_BACKEND.Models;

namespace ISM_BACKEND.Services;

public class WarehouseService
{
    private readonly DapperRepository _db;

    public WarehouseService(DapperRepository db) => _db = db;

    public async Task<long> CreateWarehouseAsync(string name)
    {
        var count = await _db.QueryFirstOrDefaultAsync<int>(IsmQueries.CountWarehouseByName, new { Name = name });
        if (count > 0)
            throw new ArgumentException($"倉庫名稱 {name} 已存在");

        _db.BeginTransaction();
        try
        {
            var id = await _db.ExecuteInsertWithIdentityAsync(IsmQueries.InsertWarehouse, new { Name = name });
            _db.Commit();
            return id;
        }
        catch
        {
            _db.Rollback();
            throw;
        }
    }

    public async Task<PagedResult<WarehouseListItem>> ListWarehousesAsync(string? name, int page, int pageSize)
    {
        var param = new { Name = name, Offset = (page - 1) * pageSize, PageSize = pageSize };
        var rows = (await _db.QueryAsync<WarehouseRow>(IsmQueries.ListWarehouses, param)).ToList();
        var total = await _db.QueryFirstOrDefaultAsync<int>(IsmQueries.CountWarehouses, new { Name = name });

        return new PagedResult<WarehouseListItem>
        {
            items = rows.Select(Map).ToList(),
            meta = new PaginationMeta { page = page, pageSize = pageSize, total = total }
        };
    }

    public async Task<WarehouseDetail?> GetWarehouseDetailAsync(long warehouseId)
    {
        var warehouse = await _db.QueryFirstOrDefaultAsync<WarehouseHeaderRow>(IsmQueries.FindWarehouseById, new { WarehouseId = warehouseId });
        if (warehouse == null)
            return null;

        var staff = (await _db.QueryAsync<StaffRow>(IsmQueries.ListWarehouseStaff, new { WarehouseId = warehouseId })).ToList();

        return new WarehouseDetail
        {
            warehouseId = warehouse.WarehouseId,
            name = warehouse.Name,
            warehouseAdmins = staff.Where(s => s.Role == (int)Role.WarehouseAdmin)
                .Select(s => new WarehouseStaffItem { userId = s.UserId, name = s.Name }).ToList(),
            warehouseUsers = staff.Where(s => s.Role == (int)Role.WarehouseUser)
                .Select(s => new WarehouseStaffItem { userId = s.UserId, name = s.Name }).ToList()
        };
    }

    private static WarehouseListItem Map(WarehouseRow row) => new()
    {
        warehouseId = row.WarehouseId,
        name = row.Name,
        warehouseAdminName = row.WarehouseAdminName,
        staffCount = row.StaffCount
    };

    private sealed class WarehouseRow
    {
        public long WarehouseId { get; set; }
        public string Name { get; set; } = "";
        public string? WarehouseAdminName { get; set; }
        public int StaffCount { get; set; }
    }

    private sealed class WarehouseHeaderRow
    {
        public long WarehouseId { get; set; }
        public string Name { get; set; } = "";
    }

    private sealed class StaffRow
    {
        public long UserId { get; set; }
        public string Name { get; set; } = "";
        public int Role { get; set; }
    }
}
