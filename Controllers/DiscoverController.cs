using dupi.Services;
using Microsoft.AspNetCore.Mvc;

namespace dupi.Controllers;

public class DiscoverController : Controller
{
    private readonly ProfileService _profileService;

    public DiscoverController(ProfileService profileService)
    {
        _profileService = profileService;
    }

    public IActionResult Index()
    {
        var profiles = _profileService.GetAllPublicProfiles();
        return View(profiles);
    }
}
