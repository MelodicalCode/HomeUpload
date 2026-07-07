using System.IO.Compression;

public static class MapPostDownloadZipEndpointFeature
{
    public static IEndpointRouteBuilder MapPostDownloadZipEndpoint(
        this IEndpointRouteBuilder app,
        Func<string, (string folder, string safePath, string downloadName)?> parseMediaReference)
    {
        app.MapPost("/api/download-zip", async (HttpRequest request) =>
        {
            if (!request.HasFormContentType)
            {
                return Results.BadRequest(new { error = "Expected multipart form upload." });
            }

            var form = await request.ReadFormAsync();
            var items = form["files"].ToArray();

            if (items.Length == 0)
            {
                return Results.BadRequest(new { error = "No files were selected for download." });
            }

            var zipStream = new MemoryStream();
            using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create, leaveOpen: true))
            {
                foreach (var item in items)
                {
                    if (string.IsNullOrWhiteSpace(item))
                    {
                        continue;
                    }

                    var parsed = parseMediaReference(item);
                    if (parsed is null)
                    {
                        continue;
                    }

                    var (_, safePath, downloadName) = parsed.Value;
                    if (!File.Exists(safePath))
                    {
                        continue;
                    }

                    var entry = archive.CreateEntry(downloadName, CompressionLevel.Fastest);
                    await using var entryStream = entry.Open();
                    await using var fileStream = File.OpenRead(safePath);
                    await fileStream.CopyToAsync(entryStream);
                }
            }

            zipStream.Position = 0;
            var zipName = $"uploads_{DateTime.UtcNow:yyyyMMdd_HHmmss}.zip";
            return Results.File(zipStream, "application/zip", zipName);
        });

        return app;
    }
}
