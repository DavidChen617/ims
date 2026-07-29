using System.Data;
using Dapper;
using Domain.InboundOrders;

namespace Infrastructure.Persistence.TypeHandlers;

public sealed class UnitPriceTypeHandler : SqlMapper.TypeHandler<UnitPrice>
{
    public override void SetValue(IDbDataParameter parameter, UnitPrice? value)
    {
        parameter.Value = value?.Value;
    }

    public override UnitPrice Parse(object value)
    {
        return UnitPrice.Create((decimal)value).Value;
    }
}