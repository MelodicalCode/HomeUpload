public static class MapGetNetworkStatusEndpointFeature
{
    public static IEndpointRouteBuilder MapGetNetworkStatusEndpoint(this IEndpointRouteBuilder app, int port)
    {
        app.MapGet("/api/system/network", () =>
        {
            var status = NetworkAddressResolver.GetNetworkStatus(port);
            return Results.Ok(status);
        });

        return app;
    }
}
