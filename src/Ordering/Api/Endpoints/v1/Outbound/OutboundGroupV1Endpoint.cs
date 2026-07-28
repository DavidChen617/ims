namespace Api.Endpoints.v1.Outbound;

public static class OutboundGroupV1Endpoint
{
    extension(RouteGroupBuilder groupBuilder)
    {
        public void MapOutboundV1Endpoints()
        {
            var outboundV1 = groupBuilder.MapGroup("orders/outbound")
                .HasApiVersion(1)
                .WithTags("OutboundV1");

            outboundV1
                .MapCreateOutboundEndpoint()
                .MapGetOutboundOrderEndpoint()
                .MapGetOutboundOrderForAdminEndpoint()
                .MapListPendingOutboundOrdersEndpoint()
                .MapListOutboundHistoryEndpoint()
                .MapListWarehouseOutboundHistoryEndpoint()
                .MapListPendingOutboundQuantitiesEndpoint()
                .MapListWarehousePendingOutboundQuantitiesEndpoint()
                .MapConfirmOutboundEndpoint()
                .MapRejectOutboundEndpoint();
        }
    }
}
