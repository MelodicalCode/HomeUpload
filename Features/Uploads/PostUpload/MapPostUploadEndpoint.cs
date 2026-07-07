public static class MapPostUploadEndpointFeature
{
    public static IEndpointRouteBuilder MapPostUploadEndpoint(
        this IEndpointRouteBuilder app,
        string photosDir,
        string videosDir,
        string filesDir,
        Func<string, string, bool> isImageFile,
        Func<string, string, bool> isVideoFile)
    {
        app.MapPost("/api/upload", async (HttpRequest request) =>
        {
            if (!request.HasFormContentType)
            {
                return Results.BadRequest(new { error = "Expected multipart form upload." });
            }

            var form = await request.ReadFormAsync();
            var files = form.Files;

            if (files.Count == 0)
            {
                return Results.BadRequest(new { error = "No files were selected." });
            }

            var saved = new List<object>();
            var rejected = new List<object>();

            foreach (var file in files)
            {
                if (file.Length == 0)
                {
                    rejected.Add(new { file = file.FileName, reason = "File is empty." });
                    continue;
                }

                var originalName = Path.GetFileName(file.FileName);
                var extension = Path.GetExtension(originalName);

                var isImage = isImageFile(file.ContentType, extension);
                var isVideo = isVideoFile(file.ContentType, extension);

                var targetFolder = isImage ? photosDir : isVideo ? videosDir : filesDir;
                var safeExtension = extension.ToLowerInvariant();
                var generatedName = $"{DateTime.UtcNow:yyyyMMddHHmmssfff}_{Guid.NewGuid():N}{safeExtension}";
                var destinationPath = Path.Combine(targetFolder, generatedName);

                await using var stream = File.Create(destinationPath);
                await file.CopyToAsync(stream);

                saved.Add(new
                {
                    originalName,
                    storedAs = generatedName,
                    type = isImage ? "photo" : isVideo ? "video" : "file",
                    bytes = file.Length
                });
            }

            return Results.Ok(new
            {
                savedCount = saved.Count,
                rejectedCount = rejected.Count,
                saved,
                rejected
            });
        });

        return app;
    }
}
