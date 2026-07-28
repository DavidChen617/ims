namespace Api.Endpoints.v1.Inbound;

public static class InboundGroupV1Endpoint
{
    extension(RouteGroupBuilder groupBuilder)
    {
        public void MapInboundV1Endpoints()
        {
            var inboundV1 = groupBuilder.MapGroup("orders/inbound")
                .HasApiVersion(1)
                .WithTags("InboundV1");

            inboundV1
                .MapCreateInboundEndpoint()
                .MapGetInboundOrderEndpoint()
                .MapGetInboundOrderForAdminEndpoint()
                .MapListPendingInboundOrdersEndpoint()
                .MapListInboundHistoryEndpoint()
                .MapListWarehouseInboundHistoryEndpoint()
                .MapListInboundOrderHistoryEndpoint()
                .MapConfirmInboundEndpoint()
                .MapRejectInboundEndpoint();
        }
    }
}
