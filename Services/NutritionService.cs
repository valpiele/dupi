using System.Text.Json;
using Azure.Storage.Blobs;
using dupi.Models;

namespace dupi.Services;

public class NutritionService
{
    private readonly BlobContainerClient _container;

    public NutritionService(IConfiguration config)
    {
        var connectionString = config["Azure:StorageConnectionString"]!;
        _container = new BlobContainerClient(connectionString, "uploads");
        try { _container.CreateIfNotExists(); } catch { }
    }

    public List<NutritionPlan> GetPlans(string userId)
    {
        var plans = new List<NutritionPlan>();
        foreach (var blob in _container.GetBlobs(prefix: $"{userId}/nutrition/"))
        {
            if (!blob.Name.EndsWith(".json")) continue;
            var content = _container.GetBlobClient(blob.Name).DownloadContent().Value.Content.ToString();
            var plan = JsonSerializer.Deserialize<NutritionPlan>(content);
            if (plan != null) plans.Add(plan);
        }
        return plans.OrderByDescending(p => p.CreatedAt).ToList();
    }

    public NutritionPlan? GetPlan(string userId, string planId)
    {
        var blob = _container.GetBlobClient($"{userId}/nutrition/{planId}.json");
        if (!blob.Exists()) return null;
        var content = blob.DownloadContent().Value.Content.ToString();
        return JsonSerializer.Deserialize<NutritionPlan>(content);
    }

    public void SavePlan(NutritionPlan plan)
    {
        var json = JsonSerializer.Serialize(plan);
        var blob = _container.GetBlobClient($"{plan.UserId}/nutrition/{plan.Id}.json");
        blob.Upload(BinaryData.FromString(json), overwrite: true);
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

    public void DeletePlan(string userId, string planId, string? fileExtension)
    {
        _container.GetBlobClient($"{userId}/nutrition/{planId}.json").DeleteIfExists();
        if (!string.IsNullOrEmpty(fileExtension))
            _container.GetBlobClient($"{userId}/nutrition/{planId}{fileExtension}").DeleteIfExists();
    }
}
