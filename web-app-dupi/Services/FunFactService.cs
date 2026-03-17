using Azure.Storage.Blobs;
using dupi.Models;
using System.Text.Json;

namespace dupi.Services;

public class FunFactService
{
    private readonly BlobContainerClient _container;
    private readonly GeminiService _gemini;

    public FunFactService(IConfiguration config, GeminiService gemini)
    {
        var connectionString = config["Azure:StorageConnectionString"]!;
        _container = new BlobContainerClient(connectionString, "uploads");
        try { _container.CreateIfNotExists(); } catch { }
        _gemini = gemini;
    }

    public async Task<DailyFunFact> GetOrGenerateAsync(
        UserProfile profile, List<Challenge> activeChallenges,
        CancellationToken ct = default)
    {
        var today = DateTime.UtcNow.ToString("yyyy-MM-dd");
        var blobName = $"{profile.UserId}/funfact-{today}.json";
        var blob = _container.GetBlobClient(blobName);

        if (blob.Exists())
        {
            var content = blob.DownloadContent().Value.Content.ToString();
            var cached = JsonSerializer.Deserialize<DailyFunFact>(content);
            if (cached != null) return cached;
        }

        var fact = await _gemini.GenerateDailyFunFactAsync(profile, activeChallenges, ct);
        blob.Upload(BinaryData.FromString(JsonSerializer.Serialize(fact)), overwrite: true);
        return fact;
    }
}
