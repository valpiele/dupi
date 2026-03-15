namespace dupi.Models;

public class NutritionPlan
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string UserId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string InputType { get; set; } = string.Empty; // "image", "pdf", "text"
    public string? OriginalFileName { get; set; }
    public string Analysis { get; set; } = string.Empty;
    public bool HasFile { get; set; }
    public string? FileExtension { get; set; }
    public bool IsPublic { get; set; }
    public List<string> SharedWithUsers { get; set; } = new();
}
