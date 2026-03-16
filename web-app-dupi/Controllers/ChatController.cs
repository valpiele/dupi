using dupi.Hubs;
using dupi.Models;
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
    private readonly ChallengeService _challengeService;

    public ChatController(ChatService chatService, SocialService socialService,
        ProfileService profileService, ChallengeService challengeService)
    {
        _chatService = chatService;
        _socialService = socialService;
        _profileService = profileService;
        _challengeService = challengeService;
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

            // Pre-fetch challenge data for any challenge messages
            var challengeData = new Dictionary<int, Challenge>();
            const string prefix = "challenge:";
            foreach (var m in messages.Where(m => m.Content.StartsWith(prefix)))
            {
                if (int.TryParse(m.Content[prefix.Length..], out var cid) && !challengeData.ContainsKey(cid))
                {
                    var c = await _challengeService.GetAsync(cid);
                    if (c != null) challengeData[cid] = c;
                }
            }
            ViewBag.ChallengeData = challengeData;

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

    // Returns the user's active/pending challenges for the share picker
    public async Task<IActionResult> ShareableChallenges()
    {
        var data = await _challengeService.GetUserChallengesAsync(UserId);
        var list = data.ActiveChallenges
            .Select(x => x.Item1)
            .Select(c =>
            {
                var (name, unit, emoji) = ChallengeMetricHelper.GetInfo(c.Metric);
                return new
                {
                    id     = c.Id,
                    title  = c.Title,
                    emoji  = emoji,
                    metric = name,
                    unit   = unit,
                    target = c.TargetValue,
                    status = c.Status.ToString().ToLower()
                };
            });
        return Json(list);
    }

    // Returns preview data for a single challenge (used by JS for real-time messages)
    public async Task<IActionResult> ChallengePreview(int id)
    {
        var c = await _challengeService.GetAsync(id);
        if (c == null) return NotFound();
        var (name, unit, emoji) = ChallengeMetricHelper.GetInfo(c.Metric);
        return Json(new
        {
            id        = c.Id,
            title     = c.Title,
            emoji     = emoji,
            metric    = name,
            unit      = unit,
            target    = c.TargetValue,
            direction = c.Direction == GoalDirection.AtLeast ? "at least" : "at most",
            status    = c.Status.ToString().ToLower(),
            endDate   = c.EndDate.ToString("MMM d"),
            url       = $"/Challenge/Dashboard/{c.Id}"
        });
    }
}
