using Api.Endpoints.Filter;
using Api.Extension;
using Davish.Result;
using Domain.Warehouse;

namespace Api.Endpoints.v1.Warehouse;

public static class CreateWarehouseEndpoint
{
    extension(RouteGroupBuilder warehouseV1Group)
    {
        public RouteGroupBuilder MapCreateWarehouseEndpoint()
        {
            warehouseV1Group.MapPost("", Handle)
                .Produces<WarehouseDto>()
                .ProducesProblem(StatusCodes.Status400BadRequest)
                .WithName("CreateWarehouse")
                .WithSummary("Create a new warehouse")
                .WithDescription("Create a new warehouse with a unique name. Returns 400 if the name already exists.")
                .RequireAuthorization("AdminOnly")
                .AddEndpointFilter<TransactionFilter>();

            return warehouseV1Group;
        }
    }

    private static async Task<IResult> Handle(
        CreateWarehouseRequest request,
        IWarehouseRepository warehouseRepository,
        CancellationToken ct)
    {
        var existing = await warehouseRepository.GetByNameAsync(request.Name, ct);

        var result = await (existing.IsSuccess
                ? Result.Failure<Domain.Warehouse.Warehouse>(new Error("Warehouse.Create", "Warehouse name already exists",
                    ErrorType.BadRequest))
                : Domain.Warehouse.Warehouse.Create(request.Name))
            .ThenAsync(async warehouse =>
            {
                await warehouseRepository.AddAsync(warehouse, ct);
                return Result.Success(new WarehouseDto(warehouse.Id, warehouse.Name));
            });

        return result.ToOk();
    }
}

public record CreateWarehouseRequest(string Name);

public record WarehouseDto(Guid Id, string Name);
