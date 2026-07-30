namespace ISM_BACKEND.Models;

public class UserListItem
{
    public long userId { get; set; }
    public string name { get; set; } = "";
    public string username { get; set; } = "";
    public string role { get; set; } = "";
    public long? warehouseId { get; set; }
    public string? warehouseName { get; set; }
    public DateTime createTime { get; set; }
}
