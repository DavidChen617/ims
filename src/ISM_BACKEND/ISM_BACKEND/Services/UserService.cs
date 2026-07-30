using ISM_BACKEND.Base;
using ISM_BACKEND.Models;

namespace ISM_BACKEND.Services;

public class UserService
{
    private readonly DapperRepository _db;

    public UserService(DapperRepository db) => _db = db;

    public async Task<PagedResult<UserListItem>> ListUsersAsync(
        string? name, string? username, string? role, long? warehouseId, int page, int pageSize)
    {
        int? roleCode = null;
        if (!string.IsNullOrEmpty(role) && Enum.TryParse<Role>(role, out var parsedRole))
            roleCode = (int)parsedRole;

        var param = new
        {
            Name = name,
            Username = username,
            Role = roleCode,
            WarehouseId = warehouseId,
            Offset = (page - 1) * pageSize,
            PageSize = pageSize
        };

        var rows = (await _db.QueryAsync<UserRow>(IsmQueries.ListUsers, param)).ToList();
        var total = await _db.QueryFirstOrDefaultAsync<int>(IsmQueries.CountUsers, param);

        return new PagedResult<UserListItem>
        {
            items = rows.Select(Map).ToList(),
            meta = new PaginationMeta { page = page, pageSize = pageSize, total = total }
        };
    }

    private static UserListItem Map(UserRow row) => new()
    {
        userId = row.UserId,
        name = row.Name,
        username = row.Username,
        role = ((Role)row.Role).ToString(),
        warehouseId = row.WarehouseId,
        warehouseName = row.WarehouseName,
        createTime = row.CreateTime
    };

    private sealed class UserRow
    {
        public long UserId { get; set; }
        public string Name { get; set; } = "";
        public string Username { get; set; } = "";
        public int Role { get; set; }
        public long? WarehouseId { get; set; }
        public string? WarehouseName { get; set; }
        public DateTime CreateTime { get; set; }
    }
}
