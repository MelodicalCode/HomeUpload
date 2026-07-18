public static class StoragePathResolver
{
    public static StoragePaths Resolve(string projectRoot)
    {
        var userName = Environment.UserName;
        var mediaRoots = new[]
        {
            Path.Combine("/run/media", userName),
            Path.Combine("/media", userName)
        };

        foreach (var mediaRoot in mediaRoots)
        {
            if (!Directory.Exists(mediaRoot))
            {
                continue;
            }

            foreach (var mountedDrive in Directory.EnumerateDirectories(mediaRoot)
                         .OrderByDescending(Directory.GetLastWriteTimeUtc))
            {
                if (TryBuildStoragePaths(Path.Combine(mountedDrive, "storage"), out var usbPaths))
                {
                    return usbPaths!;
                }
            }
        }

        return BuildStoragePaths(Path.Combine(projectRoot, "storage"));
    }

    private static bool TryBuildStoragePaths(string root, out StoragePaths? paths)
    {
        try
        {
            paths = BuildStoragePaths(root);
            return true;
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
        {
            Console.WriteLine($"[Storage] Skipping {root}: {ex.Message}");
            paths = null;
            return false;
        }
    }

    private static StoragePaths BuildStoragePaths(string root)
    {
        var paths = new StoragePaths
        {
            Root = root,
            PhotosDir = Path.Combine(root, "photos"),
            VideosDir = Path.Combine(root, "videos"),
            FilesDir = Path.Combine(root, "files")
        };

        Directory.CreateDirectory(paths.PhotosDir);
        Directory.CreateDirectory(paths.VideosDir);
        Directory.CreateDirectory(paths.FilesDir);

        return paths;
    }
}
