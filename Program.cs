using Microsoft.AspNetCore.Http.Features;

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

var storagePaths = StoragePathResolver.Resolve(app.Environment.ContentRootPath);
var fileService = new StoredFileService(storagePaths);

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGetFilesEndpoint(storagePaths.PhotosDir, storagePaths.VideosDir, storagePaths.FilesDir, fileService.EnumerateStoredFiles);
app.MapGetDownloadEndpoint(storagePaths.PhotosDir, storagePaths.VideosDir, storagePaths.FilesDir, fileService.ResolveSafePath, fileService.GetContentType);
app.MapPostDownloadZipEndpoint(fileService.ParseMediaReference);
app.MapPostDeleteFilesEndpoint(fileService.ParseMediaReference);
app.MapPostUploadEndpoint(storagePaths.PhotosDir, storagePaths.VideosDir, storagePaths.FilesDir, fileService.IsImageFile, fileService.IsVideoFile);
app.MapGetHealthEndpoint();
app.MapGetNetworkStatusEndpoint(3000);
app.MapGetStorageStatusEndpoint();

app.Lifetime.ApplicationStarted.Register(() =>
{
    StartupBanner.Write(storagePaths.Root, 3000);
});

app.Run();
