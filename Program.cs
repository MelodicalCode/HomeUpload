using Microsoft.AspNetCore.Http.Features;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.IO.Compression;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls("http://0.0.0.0:3000");
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = null;
});
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = long.MaxValue;
});

var app = builder.Build();

var storageRoot = ResolveStorageRoot(app.Environment.ContentRootPath);
var photosDir = Path.Combine(storageRoot, "photos");
var videosDir = Path.Combine(storageRoot, "videos");
var filesDir = Path.Combine(storageRoot, "files");

Directory.CreateDirectory(photosDir);
Directory.CreateDirectory(videosDir);
Directory.CreateDirectory(filesDir);

var imageExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
{
    ".jpg", ".jpeg", ".png", ".gif", ".webp", ".heic", ".heif", ".bmp"
};

var videoExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
{
    ".mp4", ".mov", ".avi", ".mkv", ".webm", ".m4v", ".3gp"
};

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/api/files", () =>
{
    var photos = EnumerateStoredFiles(photosDir, "photo");
    var videos = EnumerateStoredFiles(videosDir, "video");
    var files = EnumerateStoredFiles(filesDir, "file");

    return Results.Ok(new
    {
        items = photos.Concat(videos).Concat(files)
    });
});

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

    var path = ResolveSafePath(folder, fileName);
    if (path is null || !File.Exists(path))
    {
        return Results.NotFound();
    }

    return Results.File(path, GetContentType(path), enableRangeProcessing: true);
});

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

            var parsed = ParseMediaReference(item);
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

        var parsed = ParseMediaReference(item);
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

        var isImage = IsImageFile(file.ContentType, extension);
        var isVideo = IsVideoFile(file.ContentType, extension);

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

app.MapGet("/api/health", () => Results.Ok(new { status = "ok" }));

app.Lifetime.ApplicationStarted.Register(() =>
{
    const int port = 3000;
    var lanIp = TryGetLanIPv4();
    var localUrl = $"http://localhost:{port}";

    Console.WriteLine();
    Console.WriteLine("Upload server is running.");
    Console.WriteLine($"Storage root: {storageRoot}");
    Console.WriteLine($"Local URL: {localUrl}");

    if (lanIp is not null)
    {
        Console.WriteLine($"Phone upload URL: http://{lanIp}:{port}");
    }
    else
    {
        Console.WriteLine("Phone upload URL: Could not determine LAN IP automatically.");
    }

    Console.WriteLine();
});

app.Run();

static string? TryGetLanIPv4()
{
    foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
    {
        if (nic.OperationalStatus != OperationalStatus.Up)
        {
            continue;
        }

        if (nic.NetworkInterfaceType is NetworkInterfaceType.Loopback or NetworkInterfaceType.Tunnel)
        {
            continue;
        }

        var props = nic.GetIPProperties();

        foreach (var unicast in props.UnicastAddresses)
        {
            var address = unicast.Address;

            if (address.AddressFamily != AddressFamily.InterNetwork)
            {
                continue;
            }

            if (IPAddress.IsLoopback(address))
            {
                continue;
            }

            return address.ToString();
        }
    }

    return null;
}

static string ResolveStorageRoot(string projectRoot)
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
            var resolved = Path.Combine(mountedDrive, "storage");
            Directory.CreateDirectory(resolved);
            return resolved;
        }
    }

    var fallback = Path.Combine(projectRoot, "storage");
    Directory.CreateDirectory(fallback);
    return fallback;
}

static object[] EnumerateStoredFiles(string folder, string type)
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
            url = $"/download/{type}/{Uri.EscapeDataString(info.Name)}",
            downloadToken = $"{type}:{Uri.EscapeDataString(info.Name)}"
        })
        .Cast<object>()
        .ToArray();
}

bool IsImageFile(string contentType, string extension)
{
    return contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase)
        || imageExtensions.Contains(extension);
}

bool IsVideoFile(string contentType, string extension)
{
    return contentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase)
        || videoExtensions.Contains(extension);
}

static string GetContentType(string path)
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

static string? ResolveSafePath(string folder, string fileName)
{
    var combined = Path.GetFullPath(Path.Combine(folder, fileName));
    var root = Path.GetFullPath(folder) + Path.DirectorySeparatorChar;

    return combined.StartsWith(root, StringComparison.OrdinalIgnoreCase) ? combined : null;
}

(string folder, string safePath, string downloadName)? ParseMediaReference(string value)
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
        "photo" => photosDir,
        "video" => videosDir,
        "file" => filesDir,
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
