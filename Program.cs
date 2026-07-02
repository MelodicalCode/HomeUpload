using Microsoft.AspNetCore.Http.Features;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls("http://0.0.0.0:3000");
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 2L * 1024 * 1024 * 1024;
});

var app = builder.Build();

var storageRoot = Path.Combine(app.Environment.ContentRootPath, "storage");
var photosDir = Path.Combine(storageRoot, "photos");
var videosDir = Path.Combine(storageRoot, "videos");

Directory.CreateDirectory(photosDir);
Directory.CreateDirectory(videosDir);

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

        var isImage = file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase)
            || imageExtensions.Contains(extension);
        var isVideo = file.ContentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase)
            || videoExtensions.Contains(extension);

        if (!isImage && !isVideo)
        {
            rejected.Add(new { file = originalName, reason = "Only image/video files are allowed." });
            continue;
        }

        var targetFolder = isImage ? photosDir : videosDir;
        var safeExtension = extension.ToLowerInvariant();
        var generatedName = $"{DateTime.UtcNow:yyyyMMddHHmmssfff}_{Guid.NewGuid():N}{safeExtension}";
        var destinationPath = Path.Combine(targetFolder, generatedName);

        await using var stream = File.Create(destinationPath);
        await file.CopyToAsync(stream);

        saved.Add(new
        {
            originalName,
            storedAs = generatedName,
            type = isImage ? "photo" : "video",
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
