using dupi.Data;
using dupi.Models;
using Microsoft.EntityFrameworkCore;

namespace dupi.Services;

public class SocialService
{
    private readonly ApplicationDbContext _db;
    private readonly ProfileService _profileService;

    public SocialService(ApplicationDbContext db, ProfileService profileService)
    {
        _db = db;
        _profileService = profileService;
    }

    // ── Friends ──────────────────────────────────────────────────────────

    public async Task<List<(Friendship F, UserProfile? Profile)>> GetFriendsAsync(string userId)
    {
        var list = await _db.Friendships
            .Where(f => f.Status == FriendshipStatus.Accepted &&
                        (f.SenderId == userId || f.ReceiverId == userId))
            .OrderByDescending(f => f.RespondedAt)
            .ToListAsync();

        return list.Select(f =>
        {
            var friendId = f.SenderId == userId ? f.ReceiverId : f.SenderId;
            return (f, _profileService.GetProfileByUserId(friendId));
        }).ToList();
    }

    public async Task<List<(Friendship F, UserProfile? Profile)>> GetPendingReceivedAsync(string userId)
    {
        var list = await _db.Friendships
            .Where(f => f.ReceiverId == userId && f.Status == FriendshipStatus.Pending)
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync();

        return list.Select(f => (f, _profileService.GetProfileByUserId(f.SenderId))).ToList();
    }

    public Task<int> GetPendingCountAsync(string userId) =>
        _db.Friendships.CountAsync(f => f.ReceiverId == userId && f.Status == FriendshipStatus.Pending);

    // Returns: "none" | "friends" | "pending_sent" | "pending_received"
    public async Task<string> GetStatusAsync(string userId, string otherId)
    {
        var f = await _db.Friendships.FirstOrDefaultAsync(f =>
            (f.SenderId == userId && f.ReceiverId == otherId) ||
            (f.SenderId == otherId && f.ReceiverId == userId));

        if (f == null) return "none";
        if (f.Status == FriendshipStatus.Accepted) return "friends";
        return f.SenderId == userId ? "pending_sent" : "pending_received";
    }

    // Bulk status lookup for Discover page
    public async Task<Dictionary<string, string>> GetStatusesAsync(string userId, IEnumerable<string> otherIds)
    {
        var ids = otherIds.ToList();
        var friendships = await _db.Friendships
            .Where(f => (f.SenderId == userId && ids.Contains(f.ReceiverId)) ||
                        (f.ReceiverId == userId && ids.Contains(f.SenderId)))
            .ToListAsync();

        return ids.ToDictionary(id => id, id =>
        {
            var f = friendships.FirstOrDefault(f =>
                (f.SenderId == userId && f.ReceiverId == id) ||
                (f.SenderId == id && f.ReceiverId == userId));
            if (f == null) return "none";
            if (f.Status == FriendshipStatus.Accepted) return "friends";
            return f.SenderId == userId ? "pending_sent" : "pending_received";
        });
    }

    // ── Actions ──────────────────────────────────────────────────────────

    public async Task<bool> SendRequestAsync(string senderId, string receiverId)
    {
        if (senderId == receiverId) return false;
        var exists = await _db.Friendships.AnyAsync(f =>
            (f.SenderId == senderId && f.ReceiverId == receiverId) ||
            (f.SenderId == receiverId && f.ReceiverId == senderId));
        if (exists) return false;

        _db.Friendships.Add(new Friendship { SenderId = senderId, ReceiverId = receiverId });
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task AcceptAsync(string userId, string senderId)
    {
        var f = await _db.Friendships.FirstOrDefaultAsync(f =>
            f.SenderId == senderId && f.ReceiverId == userId && f.Status == FriendshipStatus.Pending);
        if (f == null) return;
        f.Status = FriendshipStatus.Accepted;
        f.RespondedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    public async Task DeclineAsync(string userId, string senderId)
    {
        var f = await _db.Friendships.FirstOrDefaultAsync(f =>
            f.SenderId == senderId && f.ReceiverId == userId && f.Status == FriendshipStatus.Pending);
        if (f == null) return;
        _db.Friendships.Remove(f);
        await _db.SaveChangesAsync();
    }

    public async Task UnfriendAsync(string userId, string otherId)
    {
        var f = await _db.Friendships.FirstOrDefaultAsync(f =>
            (f.SenderId == userId && f.ReceiverId == otherId) ||
            (f.SenderId == otherId && f.ReceiverId == userId));
        if (f == null) return;
        _db.Friendships.Remove(f);
        await _db.SaveChangesAsync();
    }

    public async Task<bool> AreFriendsAsync(string userId, string otherId)
    {
        return await _db.Friendships.AnyAsync(f =>
            f.Status == FriendshipStatus.Accepted &&
            ((f.SenderId == userId && f.ReceiverId == otherId) ||
             (f.SenderId == otherId && f.ReceiverId == userId)));
    }
}
