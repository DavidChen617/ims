using System.Data;
using Dapper;
using Domain.Products;

namespace Infrastructure.Persistence.TypeHandlers;

public sealed class ProductUnitTypeHandler : SqlMapper.TypeHandler<ProductUnit>
{
    public override void SetValue(IDbDataParameter parameter, ProductUnit? value)
    {
        parameter.Value = value?.Name;
    }

    public override ProductUnit Parse(object value)
    {
        return ProductUnit.Create((string)value).Value;
    }
}
