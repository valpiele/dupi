namespace dupi.Models;

public class NutritionIndexViewModel
{
    public List<NutritionPlan> Plans { get; set; } = new();
    public List<NutritionPlan> TodayPlans { get; set; } = new();
    public int CurrentStreak { get; set; }
}
