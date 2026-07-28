namespace Application;

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int TotalCount, int Page, int Size);
