public static class MapPostUploadEndpointFeature
{
    public static IEndpointRouteBuilder MapPostUploadEndpoint(
        this IEndpointRouteBuilder app,
        Func<StoragePaths> resolvePaths,
        Func<string, string, bool> isImageFile,
        Func<string, string, bool> isVideoFile)
    {
        app.MapPost("/api/upload", async (HttpRequest request) =>
        {
            if (!request.HasFormContentType)
            {
                return Results.BadRequest(new { error = "Expected multipart form upload." });
            }

            // Resolve storage paths fresh on every request so a newly connected
            // drive is picked up immediately and missing folders are auto-created.
            StoragePaths paths;
            try
            {
                paths = resolvePaths();
            }
            catch (Exception ex)
            {
                return Results.Problem(detail: ex.Message, statusCode: 503, title: "Storage unavailable.");
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

                var sizeMb = file.Length / 1_048_576.0;
                Console.WriteLine($"[Upload] Receiving: {originalName} ({sizeMb:F2} MB)");

                var isImage = isImageFile(file.ContentType, extension);
                var isVideo = isVideoFile(file.ContentType, extension);

                var targetFolder = isImage ? paths.PhotosDir : isVideo ? paths.VideosDir : paths.FilesDir;

                var freeBytes = GetAvailableFreeBytes(targetFolder);
                if (freeBytes.HasValue && freeBytes.Value < file.Length)
                {
                    var freeMb = freeBytes.Value / 1_048_576.0;
                    rejected.Add(new { file = file.FileName, reason = $"Insufficient disk space ({freeMb:F0} MB free)." });
                    continue;
                }

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

    private static long? GetAvailableFreeBytes(string path)
    {
        try
        {
            return DriveInfo.GetDrives()
                .Where(d => d.IsReady && path.StartsWith(d.RootDirectory.FullName, StringComparison.Ordinal))
                .OrderByDescending(d => d.RootDirectory.FullName.Length)
                .FirstOrDefault()
                ?.AvailableFreeSpace;
        }
        catch
        {
            return null;
        }
    }
}
