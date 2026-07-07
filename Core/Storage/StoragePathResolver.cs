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

            var mountedDrive = Directory.EnumerateDirectories(mediaRoot)
                .OrderByDescending(Directory.GetLastWriteTimeUtc)
                .FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(mountedDrive))
            {
                return BuildStoragePaths(Path.Combine(mountedDrive, "storage"));
            }
        }

        return BuildStoragePaths(Path.Combine(projectRoot, "storage"));
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
