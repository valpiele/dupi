namespace dupi.Dtos;

public class NutritionPlanDto
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string? MealType { get; set; }
    public string InputType { get; set; } = string.Empty;
    public bool HasFile { get; set; }
    public string FoodDescription { get; set; } = string.Empty;
    public int CaloriesMin { get; set; }
    public int CaloriesMax { get; set; }
    public double Proteins { get; set; }
    public double Carbohydrates { get; set; }
    public double Fats { get; set; }
    public double Fiber { get; set; }
    public double Sugar { get; set; }
    public double Sodium { get; set; }
    public List<string> WhatsGood { get; set; } = new();
    public List<string> WhatToImprove { get; set; } = new();
    public int Score { get; set; }
    public string ScoreSummary { get; set; } = string.Empty;
}

public class NutritionIndexDto
{
    public List<NutritionPlanDto> Plans { get; set; } = new();
    public List<NutritionPlanDto> TodayPlans { get; set; } = new();
    public int CurrentStreak { get; set; }
    public ActiveChallengeDto? ActiveChallenge { get; set; }
}

public class ActiveChallengeDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Metric { get; set; } = string.Empty;
    public double TargetValue { get; set; }
    public string Direction { get; set; } = string.Empty;
    public double TodayMetricValue { get; set; }
}
