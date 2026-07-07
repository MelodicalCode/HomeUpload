public static class MapGetStorageStatusEndpointFeature
{
    public static IEndpointRouteBuilder MapGetStorageStatusEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/system/storage", () =>
        {
            // Only show externally mounted USB drives (Linux: /run/media/* or /media/*)
            static bool IsUsbMount(string name) =>
                name.StartsWith("/run/media/", StringComparison.Ordinal) ||
                name.StartsWith("/media/", StringComparison.Ordinal);

            var drives = DriveInfo.GetDrives()
                .Where(d => d.IsReady
                    && d.TotalSize > 512L * 1024 * 1024
                    && d.DriveType is DriveType.Fixed or DriveType.Removable
                    && IsUsbMount(d.Name))
                .OrderBy(d => d.Name)
                .Select(d =>
                {
                    var totalGb = Math.Round(d.TotalSize / 1_073_741_824.0, 1);
                    var freeGb = Math.Round(d.AvailableFreeSpace / 1_073_741_824.0, 1);
                    var usedGb = Math.Round((d.TotalSize - d.AvailableFreeSpace) / 1_073_741_824.0, 1);
                    var usedPct = d.TotalSize > 0
                        ? (int)Math.Round((d.TotalSize - d.AvailableFreeSpace) * 100.0 / d.TotalSize)
                        : 0;
                    var label = string.IsNullOrWhiteSpace(d.VolumeLabel) ? d.Name.TrimEnd('/') : d.VolumeLabel;

                    return new { label, path = d.Name, totalGb, freeGb, usedGb, usedPct, format = d.DriveFormat };
                })
                .ToArray();

            return Results.Ok(new { drives });
        });

        return app;
    }
}
