namespace dupi.Models;

public class Project
{
    public string FileName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public DateTime UploadedAt { get; set; }

    public string FileSizeFormatted =>
        FileSizeBytes < 1024 * 1024
            ? $"{FileSizeBytes / 1024.0:F1} KB"
            : $"{FileSizeBytes / (1024.0 * 1024):F1} MB";
}
