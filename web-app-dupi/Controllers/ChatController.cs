using dupi.Hubs;
using dupi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace dupi.Controllers;

[Authorize]
public class ChatController : Controller
{
    private readonly ChatService _chatService;
    private readonly SocialService _socialService;
    private readonly ProfileService _profileService;

    public ChatController(ChatService chatService, SocialService socialService, ProfileService profileService)
    {
        _chatService = chatService;
        _socialService = socialService;
        _profileService = profileService;
    }

    private string UserId => User.FindFirstValue("dupi:uid")!;

    public async Task<IActionResult> Index(string? friendId)
    {
        var conversations = await _chatService.GetConversationsAsync(UserId);
        var friends = await _socialService.GetFriendsAsync(UserId);

        ViewBag.Conversations = conversations;
        ViewBag.ActiveFriendId = friendId;
        ViewBag.Friends = friends;
        ViewBag.OnlineFriendIds = friends
            .Select(x => x.F.SenderId == UserId ? x.F.ReceiverId : x.F.SenderId)
            .Where(ChatHub.IsOnline)
            .ToHashSet();

        if (!string.IsNullOrEmpty(friendId))
        {
            if (!await _socialService.AreFriendsAsync(UserId, friendId))
                return Forbid();

            var messages = await _chatService.GetMessagesAsync(UserId, friendId);
            ViewBag.Messages = messages;
            ViewBag.FriendProfile = _profileService.GetProfileByUserId(friendId);
            ViewBag.FriendIsOnline = ChatHub.IsOnline(friendId);

            await _chatService.MarkReadAsync(UserId, friendId);
        }

        return View();
    }

    public async Task<IActionResult> History(string friendId, int skip)
    {
        if (!await _socialService.AreFriendsAsync(UserId, friendId))
            return Forbid();

        var messages = await _chatService.GetMessagesAsync(UserId, friendId, skip);
        return Json(messages.Select(m => new
        {
            id       = m.Id,
            senderId = m.SenderId,
            content  = m.Content,
            sentAt   = m.SentAt.ToString("o"),
            isRead   = m.IsRead
        }));
    }
}
