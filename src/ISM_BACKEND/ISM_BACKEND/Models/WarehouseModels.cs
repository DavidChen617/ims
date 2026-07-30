namespace ISM_BACKEND.Models;

public class CreateWarehouseRequest
{
    public string name { get; set; } = "";
}

public class WarehouseListItem
{
    public long warehouseId { get; set; }
    public string name { get; set; } = "";
    public string? warehouseAdminName { get; set; }
    public int staffCount { get; set; }
}

public class WarehouseStaffItem
{
    public long userId { get; set; }
    public string name { get; set; } = "";
}

public class WarehouseDetail
{
    public long warehouseId { get; set; }
    public string name { get; set; } = "";
    public List<WarehouseStaffItem> warehouseAdmins { get; set; } = new();
    public List<WarehouseStaffItem> warehouseUsers { get; set; } = new();
}
