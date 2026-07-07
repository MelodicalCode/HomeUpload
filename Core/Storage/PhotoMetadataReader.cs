using MetadataExtractor;
using MetadataExtractor.Formats.Exif;

public static class PhotoMetadataReader
{
    private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".heic", ".heif", ".png", ".webp", ".tiff", ".tif"
    };

    public static DateTime? TryReadDateTaken(string filePath)
    {
        try
        {
            if (!SupportedExtensions.Contains(Path.GetExtension(filePath)))
            {
                return null;
            }

            var directories = ImageMetadataReader.ReadMetadata(filePath);

            var exifSub = directories.OfType<ExifSubIfdDirectory>().FirstOrDefault();

            if (exifSub is not null)
            {
                if (exifSub.TryGetDateTime(ExifDirectoryBase.TagDateTimeOriginal, out var dateOriginal))
                {
                    return dateOriginal;
                }

                if (exifSub.TryGetDateTime(ExifDirectoryBase.TagDateTimeDigitized, out var dateDigitized))
                {
                    return dateDigitized;
                }
            }

            var exifIfd0 = directories.OfType<ExifIfd0Directory>().FirstOrDefault();

            if (exifIfd0 is not null && exifIfd0.TryGetDateTime(ExifDirectoryBase.TagDateTime, out var dateTime))
            {
                return dateTime;
            }

            return null;
        }
        catch
        {
            return null;
        }
    }
}
