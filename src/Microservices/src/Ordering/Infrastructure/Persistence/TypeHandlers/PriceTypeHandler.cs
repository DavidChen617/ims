using System.Data;
using Dapper;
using Domain.Products;

namespace Infrastructure.Persistence.TypeHandlers;

public sealed class PriceTypeHandler : SqlMapper.TypeHandler<Price>
{
    public override void SetValue(IDbDataParameter parameter, Price? value)
    {
        parameter.Value = value?.Value;
    }

    public override Price Parse(object value)
    {
        return Price.Create((decimal)value).Value;
    }
}
