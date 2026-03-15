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
