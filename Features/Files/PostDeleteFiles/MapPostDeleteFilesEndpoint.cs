public static class MapPostDeleteFilesEndpointFeature
{
    public static IEndpointRouteBuilder MapPostDeleteFilesEndpoint(
        this IEndpointRouteBuilder app,
        Func<string, (string folder, string safePath, string downloadName)?> parseMediaReference)
    {
        app.MapPost("/api/delete-files", async (HttpRequest request) =>
        {
            if (!request.HasFormContentType)
            {
                return Results.BadRequest(new { error = "Expected multipart form upload." });
            }

            var form = await request.ReadFormAsync();
            var items = form["files"].ToArray();

            if (items.Length == 0)
            {
                return Results.BadRequest(new { error = "No files were selected for deletion." });
            }

            var deleted = 0;
            var skipped = 0;

            foreach (var item in items)
            {
                if (string.IsNullOrWhiteSpace(item))
                {
                    skipped++;
                    continue;
                }

                var parsed = parseMediaReference(item);
                if (parsed is null)
                {
                    skipped++;
                    continue;
                }

                var (_, safePath, _) = parsed.Value;
                if (!File.Exists(safePath))
                {
                    skipped++;
                    continue;
                }

                File.Delete(safePath);
                deleted++;
            }

            return Results.Ok(new
            {
                deletedCount = deleted,
                skippedCount = skipped
            });
        });

        return app;
    }
}
