using dupi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace dupi.Controllers;

[Authorize]
public class FriendsController : Controller
{
    private readonly SocialService _socialService;
    private readonly ChatService _chatService;

    public FriendsController(SocialService socialService, ChatService chatService)
    {
        _socialService = socialService;
        _chatService = chatService;
    }

    private string UserId => User.FindFirstValue("dupi:uid")!;

    public async Task<IActionResult> Index()
    {
        var friends = await _socialService.GetFriendsAsync(UserId);
        ViewBag.Requests = await _socialService.GetPendingReceivedAsync(UserId);
        return View(friends);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SendRequest(string receiverId, string returnUrl = "/Friends")
    {
        await _socialService.SendRequestAsync(UserId, receiverId);
        return Redirect(returnUrl);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Accept(string senderId)
    {
        await _socialService.AcceptAsync(UserId, senderId);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Decline(string senderId)
    {
        await _socialService.DeclineAsync(UserId, senderId);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Unfriend(string otherId, string returnUrl = "/Friends")
    {
        await _socialService.UnfriendAsync(UserId, otherId);
        return Redirect(returnUrl);
    }

    public async Task<IActionResult> Count()
    {
        var pending = await _socialService.GetPendingCountAsync(UserId);
        var unread = await _chatService.GetUnreadCountAsync(UserId);
        return Json(new { pending, unread });
    }
}
