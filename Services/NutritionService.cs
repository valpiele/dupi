using Azure.Storage.Blobs;
using dupi.Data;
using dupi.Models;
using Microsoft.EntityFrameworkCore;

namespace dupi.Services;

public class NutritionService
{
    private readonly ApplicationDbContext _db;
    private readonly BlobContainerClient _container;

    public NutritionService(ApplicationDbContext db, IConfiguration config)
    {
        _db = db;
        var connectionString = config["Azure:StorageConnectionString"]!;
        _container = new BlobContainerClient(connectionString, "uploads");
        try { _container.CreateIfNotExists(); } catch { }
    }

    public Task<List<NutritionPlan>> GetPlansAsync(string userId) =>
        _db.NutritionPlans
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

    public async Task<string?> GetRecentContextSummaryAsync(string userId)
    {
        var since = DateTime.UtcNow.AddDays(-7);
        var recent = await _db.NutritionPlans
            .Where(p => p.UserId == userId && p.CreatedAt >= since && p.CaloriesMin > 0)
            .OrderBy(p => p.CreatedAt)
            .ToListAsync();

        if (recent.Count == 0) return null;

        var days = recent
            .GroupBy(p => p.CreatedAt.Date)
            .OrderBy(g => g.Key)
            .ToList();

        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"Meals logged in the past 7 days: {recent.Count} across {days.Count} day(s).");

        var avgCalories = recent.Average(p => (p.CaloriesMin + p.CaloriesMax) / 2.0);
        var avgProtein  = recent.Average(p => p.Proteins);
        var avgCarbs    = recent.Average(p => p.Carbohydrates);
        var avgFat      = recent.Average(p => p.Fats);
        sb.AppendLine($"Average per meal: {avgCalories:0} kcal | Protein {avgProtein:0}g | Carbs {avgCarbs:0}g | Fat {avgFat:0}g");

        var scoredMeals = recent.Where(p => p.Score > 0).ToList();
        if (scoredMeals.Count > 0)
            sb.AppendLine($"Average meal score: {scoredMeals.Average(p => p.Score):0.1}/10");

        sb.AppendLine("Recent meals:");
        foreach (var p in recent.TakeLast(7))
        {
            var mealLabel = string.IsNullOrEmpty(p.MealType) ? "" : $" [{p.MealType}]";
            sb.AppendLine($"- {p.CreatedAt:MMM d}{mealLabel}: {p.FoodDescription} (~{(p.CaloriesMin + p.CaloriesMax) / 2} kcal, P:{p.Proteins:0}g C:{p.Carbohydrates:0}g F:{p.Fats:0}g, score {p.Score}/10)");
        }

        return sb.ToString();
    }

    public Task<NutritionPlan?> GetPlanAsync(string userId, string planId) =>
        _db.NutritionPlans
            .FirstOrDefaultAsync(p => p.UserId == userId && p.Id == planId);

    public async Task SavePlanAsync(NutritionPlan plan)
    {
        var existing = await _db.NutritionPlans.FindAsync(plan.Id);
        if (existing == null)
            _db.NutritionPlans.Add(plan);
        else
            _db.Entry(existing).CurrentValues.SetValues(plan);
        await _db.SaveChangesAsync();
    }

    public async Task SaveFileAsync(string userId, string planId, string extension, Stream content)
    {
        var blob = _container.GetBlobClient($"{userId}/nutrition/{planId}{extension}");
        await blob.UploadAsync(content, overwrite: true);
    }

    public Stream? GetFile(string userId, string planId, string extension)
    {
        var blob = _container.GetBlobClient($"{userId}/nutrition/{planId}{extension}");
        if (!blob.Exists()) return null;
        return blob.DownloadStreaming().Value.Content;
    }

    public async Task DeletePlanAsync(string userId, string planId)
    {
        var plan = await _db.NutritionPlans
            .FirstOrDefaultAsync(p => p.UserId == userId && p.Id == planId);
        if (plan == null) return;

        var fileExtension = plan.FileExtension;
        _db.NutritionPlans.Remove(plan);
        await _db.SaveChangesAsync();

        if (!string.IsNullOrEmpty(fileExtension))
            _container.GetBlobClient($"{userId}/nutrition/{planId}{fileExtension}").DeleteIfExists();
    }
}
