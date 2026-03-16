namespace dupi.Models;

public enum ChallengeType { FriendChallenge, Community }
public enum ChallengeStatus { Pending, Active, Completed }

public enum ChallengeMetric
{
    Protein,
    Carbohydrates,
    Fats,
    Fiber,
    Sugar,
    Sodium,
    Calories,
    Score,
    MealCount
}

public enum GoalDirection
{
    AtLeast,
    AtMost
}

public class Challenge
{
    public int Id { get; set; }
    public string CreatorId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ChallengeMetric Metric { get; set; } = ChallengeMetric.Protein;
    public double TargetValue { get; set; } = 120;
    public GoalDirection Direction { get; set; } = GoalDirection.AtLeast;
    public ChallengeType Type { get; set; }
    public ChallengeStatus Status { get; set; } = ChallengeStatus.Pending;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public static class ChallengeMetricHelper
{
    public static double ExtractValue(NutritionPlan plan, ChallengeMetric metric) => metric switch
    {
        ChallengeMetric.Protein => plan.Proteins,
        ChallengeMetric.Carbohydrates => plan.Carbohydrates,
        ChallengeMetric.Fats => plan.Fats,
        ChallengeMetric.Fiber => plan.Fiber,
        ChallengeMetric.Sugar => plan.Sugar,
        ChallengeMetric.Sodium => plan.Sodium,
        ChallengeMetric.Calories => (plan.CaloriesMin + plan.CaloriesMax) / 2.0,
        ChallengeMetric.Score => plan.Score,
        ChallengeMetric.MealCount => 1,
        _ => 0
    };

    public static bool IsTargetHit(double value, double target, GoalDirection direction) => direction switch
    {
        GoalDirection.AtLeast => value >= target,
        GoalDirection.AtMost => value <= target && value > 0,
        _ => false
    };

    public static GoalDirection DefaultDirection(ChallengeMetric metric) => metric switch
    {
        ChallengeMetric.Sugar => GoalDirection.AtMost,
        ChallengeMetric.Sodium => GoalDirection.AtMost,
        _ => GoalDirection.AtLeast
    };

    public static (string Name, string Unit, string Emoji) GetInfo(ChallengeMetric metric) => metric switch
    {
        ChallengeMetric.Protein => ("Protein", "g", "🥩"),
        ChallengeMetric.Carbohydrates => ("Carbs", "g", "🍞"),
        ChallengeMetric.Fats => ("Fat", "g", "🥑"),
        ChallengeMetric.Fiber => ("Fiber", "g", "🥦"),
        ChallengeMetric.Sugar => ("Sugar", "g", "🍬"),
        ChallengeMetric.Sodium => ("Sodium", "mg", "🧂"),
        ChallengeMetric.Calories => ("Calories", "kcal", "🔥"),
        ChallengeMetric.Score => ("Score", "/10", "⭐"),
        ChallengeMetric.MealCount => ("Meals", "meals", "🍽️"),
        _ => ("Unknown", "", "❓")
    };

    public static (double Default, double Min, double Max, double Step) GetRange(ChallengeMetric metric) => metric switch
    {
        ChallengeMetric.Protein => (120, 10, 300, 5),
        ChallengeMetric.Carbohydrates => (200, 50, 500, 10),
        ChallengeMetric.Fats => (65, 20, 200, 5),
        ChallengeMetric.Fiber => (25, 5, 60, 1),
        ChallengeMetric.Sugar => (50, 10, 200, 5),
        ChallengeMetric.Sodium => (2300, 500, 5000, 100),
        ChallengeMetric.Calories => (2000, 500, 5000, 50),
        ChallengeMetric.Score => (7, 1, 10, 1),
        ChallengeMetric.MealCount => (3, 1, 10, 1),
        _ => (100, 1, 1000, 1)
    };
}
