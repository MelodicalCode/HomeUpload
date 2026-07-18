using Microsoft.AspNetCore.Http.Features;

// Disable inotify-based config reloading — not needed for a kiosk and
// exhausts the kernel's inotify limit on resource-constrained systems (Pi).
Environment.SetEnvironmentVariable("DOTNET_USE_POLLING_FILE_WATCHER", "1");

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls("http://0.0.0.0:3000");
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = null;
    // Longer than Chrome's ~300s pool timeout so Chrome always closes idle
    // connections first. Prevents stale-connection retry on reconnect and
    // stops Kestrel from closing the connection while a file picker is open.
    options.Limits.KeepAliveTimeout = TimeSpan.FromSeconds(600);
});
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = long.MaxValue;
});

var app = builder.Build();

var storagePaths = StoragePathResolver.Resolve(app.Environment.ContentRootPath);
var fileService = new StoredFileService(storagePaths);

app.UseDefaultFiles();
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        // HTML: short cache so refreshes pick up server restarts quickly.
        // Assets (JS, etc.): longer cache so reconnects serve instantly from memory.
        var isHtml = ctx.File.Name.EndsWith(".html", StringComparison.OrdinalIgnoreCase);
        ctx.Context.Response.Headers["Cache-Control"] = isHtml
            ? "public, max-age=60"
            : "public, max-age=604800";
    }
});

app.MapGetFilesEndpoint(storagePaths.PhotosDir, storagePaths.VideosDir, storagePaths.FilesDir, fileService.EnumerateStoredFiles);
app.MapGetDownloadEndpoint(storagePaths.PhotosDir, storagePaths.VideosDir, storagePaths.FilesDir, fileService.ResolveSafePath, fileService.GetContentType);
app.MapPostDownloadZipEndpoint(fileService.ParseMediaReference);
app.MapPostDeleteFilesEndpoint(fileService.ParseMediaReference);
app.MapPostUploadEndpoint(() => StoragePathResolver.Resolve(app.Environment.ContentRootPath), fileService.IsImageFile, fileService.IsVideoFile);
app.MapGetHealthEndpoint();
app.MapGetNetworkStatusEndpoint(3000);
app.MapGetStorageStatusEndpoint();

app.Lifetime.ApplicationStarted.Register(() =>
{
    StartupBanner.Write(storagePaths.Root, 3000);
});

app.Run();
