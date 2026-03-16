using dupi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace dupi.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private readonly ChatService _chatService;
    private readonly SocialService _socialService;

    public ChatHub(ChatService chatService, SocialService socialService)
    {
        _chatService = chatService;
        _socialService = socialService;
    }

    private string UserId => Context.User!.FindFirstValue("dupi:uid")!;

    public override async Task OnConnectedAsync()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, UserId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, UserId);
        await base.OnDisconnectedAsync(exception);
    }

    public async Task SendMessage(string receiverId, string content)
    {
        if (string.IsNullOrWhiteSpace(content) || content.Length > 2000) return;

        // Only friends can message each other
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
}
