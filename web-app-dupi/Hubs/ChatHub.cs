using dupi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using System.Security.Claims;

namespace dupi.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private readonly ChatService _chatService;
    private readonly SocialService _socialService;

    // userId → number of active connections (multi-tab support)
    private static readonly ConcurrentDictionary<string, int> _onlineCount = new();

    public ChatHub(ChatService chatService, SocialService socialService)
    {
        _chatService = chatService;
        _socialService = socialService;
    }

    private string UserId => Context.User!.FindFirstValue("dupi:uid")!;

    public static bool IsOnline(string userId) =>
        _onlineCount.TryGetValue(userId, out var c) && c > 0;

    public override async Task OnConnectedAsync()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, UserId);
        var count = _onlineCount.AddOrUpdate(UserId, 1, (_, c) => c + 1);
        if (count == 1)
            await NotifyFriendsPresenceAsync("UserOnline");
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, UserId);
        var count = _onlineCount.AddOrUpdate(UserId, 0, (_, c) => Math.Max(0, c - 1));
        if (count == 0)
            await NotifyFriendsPresenceAsync("UserOffline");
        await base.OnDisconnectedAsync(exception);
    }

    private async Task NotifyFriendsPresenceAsync(string eventName)
    {
        var friends = await _socialService.GetFriendsAsync(UserId);
        var tasks = friends.Select(x =>
        {
            var friendId = x.F.SenderId == UserId ? x.F.ReceiverId : x.F.SenderId;
            return Clients.Group(friendId).SendAsync(eventName, UserId);
        });
        await Task.WhenAll(tasks);
    }

    public async Task SendMessage(string receiverId, string content)
    {
        if (string.IsNullOrWhiteSpace(content) || content.Length > 2000) return;
        if (!await _socialService.AreFriendsAsync(UserId, receiverId)) return;

        var message = await _chatService.SendMessageAsync(UserId, receiverId, content.Trim());

        var payload = new
        {
            id       = message.Id,
            senderId = message.SenderId,
            content  = message.Content,
            sentAt   = message.SentAt.ToString("o")
        };

        await Clients.Group(receiverId).SendAsync("ReceiveMessage", payload);
        await Clients.Group(UserId).SendAsync("ReceiveMessage", payload);
    }

    public async Task MarkRead(string friendId)
    {
        await _chatService.MarkReadAsync(UserId, friendId);
        await Clients.Group(friendId).SendAsync("MessagesRead", UserId);
    }

    public Task StartTyping(string receiverId) =>
        Clients.Group(receiverId).SendAsync("TypingStarted", UserId);

    public Task StopTyping(string receiverId) =>
        Clients.Group(receiverId).SendAsync("TypingStopped", UserId);
}
