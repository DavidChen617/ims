namespace ISM_BACKEND.Models;

public class PaginationMeta
{
    public int page { get; set; }
    public int pageSize { get; set; }
    public int total { get; set; }
}

public class PagedResult<T>
{
    public List<T> items { get; set; } = new();
    public PaginationMeta meta { get; set; } = new();
}
