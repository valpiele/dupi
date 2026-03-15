using dupi.Services;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace dupi.ViewComponents;

public class SocialBadgesViewComponent : ViewComponent
{
    private readonly SocialService _socialService;
    private readonly ChatService _chatService;

    public SocialBadgesViewComponent(SocialService socialService, ChatService chatService)
    {
        _socialService = socialService;
        _chatService = chatService;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var userId = UserClaimsPrincipal.FindFirstValue("dupi:uid");
        if (string.IsNullOrEmpty(userId)) return Content(string.Empty);

        ViewBag.Pending = await _socialService.GetPendingCountAsync(userId);
        ViewBag.Unread = await _chatService.GetUnreadCountAsync(userId);
        return View();
    }
}
