public sealed class StoredFileService
{
    private readonly StoragePaths _paths;

    private static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".gif", ".webp", ".heic", ".heif", ".bmp"
    };

    private static readonly HashSet<string> VideoExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mp4", ".mov", ".avi", ".mkv", ".webm", ".m4v", ".3gp"
    };

    public StoredFileService(StoragePaths paths)
    {
        _paths = paths;
    }

    public object[] EnumerateStoredFiles(string folder, string type)
    {
        if (!Directory.Exists(folder))
        {
            return [];
        }

        return Directory.EnumerateFiles(folder)
            .OrderByDescending(File.GetCreationTimeUtc)
            .Select(path => new FileInfo(path))
            .Select(info => new
            {
                type,
                name = info.Name,
                size = info.Length,
                createdUtc = info.CreationTimeUtc,
                takenAt = type == "photo" ? PhotoMetadataReader.TryReadDateTaken(info.FullName) : (DateTime?)null,
                url = $"/download/{type}/{Uri.EscapeDataString(info.Name)}",
                downloadToken = $"{type}:{Uri.EscapeDataString(info.Name)}"
            })
            .Cast<object>()
            .ToArray();
    }

    public bool IsImageFile(string contentType, string extension)
    {
        return contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase)
            || ImageExtensions.Contains(extension);
    }

    public bool IsVideoFile(string contentType, string extension)
    {
        return contentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase)
            || VideoExtensions.Contains(extension);
    }

    public string GetContentType(string path)
    {
        var extension = Path.GetExtension(path).ToLowerInvariant();

        return extension switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            ".heic" => "image/heic",
            ".heif" => "image/heif",
            ".bmp" => "image/bmp",
            ".mp4" => "video/mp4",
            ".mov" => "video/quicktime",
            ".avi" => "video/x-msvideo",
            ".mkv" => "video/x-matroska",
            ".webm" => "video/webm",
            ".m4v" => "video/x-m4v",
            ".3gp" => "video/3gpp",
            _ => "application/octet-stream"
        };
    }

    public string? ResolveSafePath(string folder, string fileName)
    {
        var combined = Path.GetFullPath(Path.Combine(folder, fileName));
        var root = Path.GetFullPath(folder) + Path.DirectorySeparatorChar;

        return combined.StartsWith(root, StringComparison.OrdinalIgnoreCase) ? combined : null;
    }

    public (string folder, string safePath, string downloadName)? ParseMediaReference(string value)
    {
        var separatorIndex = value.IndexOf(':');
        if (separatorIndex <= 0 || separatorIndex == value.Length - 1)
        {
            return null;
        }

        var type = value[..separatorIndex];
        var encodedName = value[(separatorIndex + 1)..];
        var fileName = Uri.UnescapeDataString(encodedName);

        var folder = type.ToLowerInvariant() switch
        {
            "photo" => _paths.PhotosDir,
            "video" => _paths.VideosDir,
            "file" => _paths.FilesDir,
            _ => null
        };

        if (folder is null)
        {
            return null;
        }

        var safePath = ResolveSafePath(folder, fileName);
        if (safePath is null)
        {
            return null;
        }

        return (folder, safePath, fileName);
    }
}
