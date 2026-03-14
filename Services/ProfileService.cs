using System.Text.Json;
using System.Text.RegularExpressions;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using dupi.Models;

namespace dupi.Services;

public class ProfileService
{
    private readonly BlobContainerClient _container;

    public ProfileService(IConfiguration config)
    {
        var connectionString = config["Azure:StorageConnectionString"]!;
        _container = new BlobContainerClient(connectionString, "uploads");
        _container.CreateIfNotExists();
    }

    // ---------- Profile ----------

    public UserProfile GetProfile(string userId, string email, string displayName)
    {
        var blob = _container.GetBlobClient($"{userId}/profile.json");
        if (blob.Exists())
        {
            var content = blob.DownloadContent().Value.Content.ToString();
            var profile = JsonSerializer.Deserialize<UserProfile>(content);
            if (profile != null) return profile;
        }
        return new UserProfile
        {
            UserId = userId,
            Username = SuggestUsername(displayName, userId),
            Email = email,
            DisplayName = displayName,
            IsPublic = false
        };
    }

    public void SaveProfile(UserProfile profile)
    {
        var json = JsonSerializer.Serialize(profile);
        var blob = _container.GetBlobClient($"{profile.UserId}/profile.json");
        blob.Upload(BinaryData.FromString(json), overwrite: true);
    }

    public UserProfile? GetProfileByUsername(string username)
    {
        return AllProfiles().FirstOrDefault(p =>
            string.Equals(p.Username, username, StringComparison.OrdinalIgnoreCase));
    }

    public UserProfile? GetPublicProfileByUsername(string username)
    {
        var profile = GetProfileByUsername(username);
        return profile?.IsPublic == true ? profile : null;
    }

    public bool IsUsernameTaken(string username, string excludeUserId)
    {
        return AllProfiles().Any(p =>
            !string.Equals(p.UserId, excludeUserId, StringComparison.Ordinal) &&
            string.Equals(p.Username, username, StringComparison.OrdinalIgnoreCase));
    }

    public static bool IsValidUsername(string username) =>
        !string.IsNullOrWhiteSpace(username) &&
        username.Length >= 3 &&
        username.Length <= 30 &&
        Regex.IsMatch(username, @"^[a-zA-Z0-9_-]+$");

    public List<(UserProfile Profile, int ProjectCount)> GetAllPublicProfiles()
    {
        return AllProfiles()
            .Where(p => p.IsPublic && !string.IsNullOrEmpty(p.Username))
            .Select(p => (p, GetProjects(p.UserId).Count))
            .ToList();
    }

    // ---------- Projects ----------

    public List<Project> GetProjects(string userId)
    {
        return _container.GetBlobs(prefix: $"{userId}/")
            .Where(b => b.Name.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            .Select(b => new Project
            {
                FileName = Path.GetFileName(b.Name),
                DisplayName = Path.GetFileNameWithoutExtension(b.Name),
                FileSizeBytes = b.Properties.ContentLength ?? 0,
                UploadedAt = b.Properties.CreatedOn?.LocalDateTime ?? DateTime.MinValue
            })
            .OrderByDescending(p => p.UploadedAt)
            .ToList();
    }

    public async Task UploadProjectAsync(string userId, string fileName, Stream content)
    {
        var blob = _container.GetBlobClient($"{userId}/{fileName}");
        await blob.UploadAsync(content, overwrite: true);
    }

    public Stream DownloadProject(string userId, string fileName)
    {
        var blob = _container.GetBlobClient($"{userId}/{Path.GetFileName(fileName)}");
        var response = blob.DownloadStreaming();
        return response.Value.Content;
    }

    public bool ProjectExists(string userId, string fileName)
    {
        return _container.GetBlobClient($"{userId}/{Path.GetFileName(fileName)}").Exists();
    }

    public void DeleteProject(string userId, string fileName)
    {
        _container.GetBlobClient($"{userId}/{Path.GetFileName(fileName)}").DeleteIfExists();
    }

    // ---------- Helpers ----------

    private IEnumerable<UserProfile> AllProfiles()
    {
        foreach (var blob in _container.GetBlobs())
        {
            if (!blob.Name.EndsWith("/profile.json")) continue;
            var content = _container.GetBlobClient(blob.Name).DownloadContent().Value.Content.ToString();
            var profile = JsonSerializer.Deserialize<UserProfile>(content);
            if (profile != null) yield return profile;
        }
    }

    private static string SuggestUsername(string displayName, string userId)
    {
        var suggestion = Regex.Replace(displayName.ToLower(), @"[^a-z0-9]", "-");
        suggestion = Regex.Replace(suggestion, @"-+", "-").Trim('-');
        return string.IsNullOrEmpty(suggestion) ? $"user-{userId[..6]}" : suggestion;
    }
}
