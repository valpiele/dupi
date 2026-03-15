namespace dupi.Models;

public class NutritionPlan
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string UserId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string InputType { get; set; } = string.Empty; // "image", "pdf", "text"
    public string? MealType { get; set; } // "breakfast", "lunch", "dinner", "snack", "other"
    public bool HasFile { get; set; }
    public string? FileExtension { get; set; }
    public bool IsPublic { get; set; }
    public List<string> SharedWithUsers { get; set; } = new();

    // Structured analysis — always populated by Gemini
    public string FoodDescription { get; set; } = string.Empty;
    public int CaloriesMin { get; set; }
    public int CaloriesMax { get; set; }
    public double Proteins { get; set; }
    public double Carbohydrates { get; set; }
    public double Fats { get; set; }
    public List<string> WhatsGood { get; set; } = new();
    public List<string> WhatToImprove { get; set; } = new();
    public int Score { get; set; }
    public string ScoreSummary { get; set; } = string.Empty;
}
