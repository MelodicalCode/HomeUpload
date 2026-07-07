public static class MapGetDownloadEndpointFeature
{
    public static IEndpointRouteBuilder MapGetDownloadEndpoint(
        this IEndpointRouteBuilder app,
        string photosDir,
        string videosDir,
        string filesDir,
        Func<string, string, string?> resolveSafePath,
        Func<string, string> getContentType)
    {
        app.MapGet("/download/{type}/{fileName}", (string type, string fileName) =>
        {
            var folder = type.ToLowerInvariant() switch
            {
                "photo" => photosDir,
                "video" => videosDir,
                "file" => filesDir,
                _ => null
            };

            if (folder is null)
            {
                return Results.NotFound();
            }

            var path = resolveSafePath(folder, fileName);
            if (path is null || !File.Exists(path))
            {
                return Results.NotFound();
            }

            return Results.File(path, getContentType(path), enableRangeProcessing: true);
        });

        return app;
    }
}
