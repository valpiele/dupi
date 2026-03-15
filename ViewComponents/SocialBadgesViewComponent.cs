using dupi.Services;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace dupi.ViewComponents;

public class SocialBadgesViewComponent : ViewComponent
{
    private readonly SocialService _socialService;
    private readonly ChatService _chatService;
    private readonly ChallengeService _challengeService;

    public SocialBadgesViewComponent(SocialService socialService, ChatService chatService, ChallengeService challengeService)
    {
        _socialService = socialService;
        _chatService = chatService;
        _challengeService = challengeService;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var userId = UserClaimsPrincipal.FindFirstValue("dupi:uid");
        if (string.IsNullOrEmpty(userId)) return Content(string.Empty);

        ViewBag.Pending = await _socialService.GetPendingCountAsync(userId);
        ViewBag.Unread = await _chatService.GetUnreadCountAsync(userId);
        ViewBag.ChallengeInvites = await _challengeService.GetPendingInviteCountAsync(userId);
        return View();
    }
}
