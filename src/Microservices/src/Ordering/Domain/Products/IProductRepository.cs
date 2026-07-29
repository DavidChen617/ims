using Davish.Result;

namespace Domain.Products;

public interface IProductRepository
{
    Task<Result> AddAsync(Product product, CancellationToken ct);
    Task<Result<IReadOnlyList<Guid>>> GetExistingIdsAsync(IReadOnlyList<Guid> ids, CancellationToken ct);
    Task<Result<IReadOnlyList<Product>>> GetByIdsAsync(IReadOnlyList<Guid> ids, CancellationToken ct);
    Task<Result<Product>> GetByNoAsync(string productNo, CancellationToken ct);
    Task<Result> AddUnitAsync(ProductUnit unit, CancellationToken ct);
    Task<Result> DeleteUnitAsync(string name, CancellationToken ct);
    Task<Result<ProductUnit>> GetUnitByNameAsync(string name, CancellationToken ct);
    Task<Result<bool>> IsUnitInUseAsync(string name, CancellationToken ct);
}
