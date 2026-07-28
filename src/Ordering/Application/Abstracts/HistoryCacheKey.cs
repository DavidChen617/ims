namespace Application.Abstracts;

// Admin 查全倉庫(沒篩選 warehouseId)時,用這個固定字串取代空字串當 cache key 的一部分——
// 這樣「全倉庫」這個查詢結果也會是一個可以被明確 DeleteByPrefixAsync 失效的 key,
// 不會變成 prefix 比對不到、只能靠 TTL 兜底的孤兒快取。
public static class HistoryCacheKey
{
    public const string AllWarehouses = "all";

    public static string WarehouseSegment(Guid? warehouseId) => warehouseId?.ToString() ?? AllWarehouses;
}
