using dupi.Data;
using dupi.Models;
using Microsoft.EntityFrameworkCore;

namespace dupi.Services;

public class ChatService
{
    private readonly ApplicationDbContext _db;
    private readonly ProfileService _profileService;

    public ChatService(ApplicationDbContext db, ProfileService profileService)
    {
        _db = db;
        _profileService = profileService;
    }

    public async Task<List<(string FriendId, UserProfile? Profile, Message Last, int Unread)>> GetConversationsAsync(string userId)
    {
        var messages = await _db.Messages
            .Where(m => m.SenderId == userId || m.ReceiverId == userId)
            .OrderByDescending(m => m.SentAt)
            .ToListAsync();

        var convos = messages
            .GroupBy(m => m.SenderId == userId ? m.ReceiverId : m.SenderId)
            .Select(g => (
                FriendId: g.Key,
                Last: g.First(),
                Unread: g.Count(m => m.ReceiverId == userId && !m.IsRead)))
            .OrderByDescending(c => c.Last.SentAt)
            .ToList();

        return convos.Select(c => (c.FriendId, _profileService.GetProfileByUserId(c.FriendId), c.Last, c.Unread)).ToList();
    }

    public async Task<List<Message>> GetMessagesAsync(string userId, string friendId, int skip = 0, int take = 60)
    {
        var msgs = await _db.Messages
            .Where(m => (m.SenderId == userId && m.ReceiverId == friendId) ||
                        (m.SenderId == friendId && m.ReceiverId == userId))
            .OrderByDescending(m => m.SentAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync();

        return msgs.OrderBy(m => m.SentAt).ToList();
    }

    public async Task<Message> SendMessageAsync(string senderId, string receiverId, string content)
    {
        var msg = new Message { SenderId = senderId, ReceiverId = receiverId, Content = content.Trim() };
        _db.Messages.Add(msg);
        await _db.SaveChangesAsync();
        return msg;
    }

    public async Task MarkReadAsync(string receiverId, string senderId)
    {
        var unread = await _db.Messages
            .Where(m => m.SenderId == senderId && m.ReceiverId == receiverId && !m.IsRead)
            .ToListAsync();
        foreach (var m in unread) m.IsRead = true;
        if (unread.Count > 0) await _db.SaveChangesAsync();
    }

    public Task<int> GetUnreadCountAsync(string userId) =>
        _db.Messages.CountAsync(m => m.ReceiverId == userId && !m.IsRead);
}
