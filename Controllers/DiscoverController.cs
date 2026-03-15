using dupi.Services;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace dupi.Controllers;

public class DiscoverController : Controller
{
    private readonly ProfileService _profileService;
    private readonly SocialService _socialService;

    public DiscoverController(ProfileService profileService, SocialService socialService)
    {
        _profileService = profileService;
        _socialService = socialService;
    }

    public async Task<IActionResult> Index()
    {
        var profiles = _profileService.GetAllPublicProfiles();

        var userId = User.FindFirstValue("dupi:uid");
        if (!string.IsNullOrEmpty(userId))
        {
            var ids = profiles.Select(p => p.Profile.UserId).Where(id => id != userId);
            ViewBag.FriendStatuses = await _socialService.GetStatusesAsync(userId, ids);
            ViewBag.CurrentUserId = userId;
        }

        return View(profiles);
    }
}
