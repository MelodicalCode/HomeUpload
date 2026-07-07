public static class MapGetHealthEndpointFeature
{
    public static IEndpointRouteBuilder MapGetHealthEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/health", () => Results.Ok(new { status = "ok" }));
        return app;
    }
}
