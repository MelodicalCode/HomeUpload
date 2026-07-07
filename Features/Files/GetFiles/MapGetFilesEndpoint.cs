public static class MapGetFilesEndpointFeature
{
    public static IEndpointRouteBuilder MapGetFilesEndpoint(
        this IEndpointRouteBuilder app,
        string photosDir,
        string videosDir,
        string filesDir,
        Func<string, string, object[]> enumerateStoredFiles)
    {
        app.MapGet("/api/files", () =>
        {
            var photos = enumerateStoredFiles(photosDir, "photo");
            var videos = enumerateStoredFiles(videosDir, "video");
            var files = enumerateStoredFiles(filesDir, "file");

            return Results.Ok(new
            {
                items = photos.Concat(videos).Concat(files)
            });
        });

        return app;
    }
}
