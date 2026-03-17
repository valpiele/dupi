using dupi.Models;
using dupi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;

namespace dupi.Controllers;

public class ProfileController : Controller
{
    private readonly ProfileService _profileService;
    private readonly SocialService _socialService;
    private readonly ChallengeService _challengeService;
    private readonly FunFactService _funFactService;

    public ProfileController(
        ProfileService profileService,
        SocialService socialService,
        ChallengeService challengeService,
        FunFactService funFactService)
    {
        _profileService = profileService;
        _socialService = socialService;
        _challengeService = challengeService;
        _funFactService = funFactService;
    }

    private string UserId => User.FindFirstValue("dupi:uid")!;

    [Authorize]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var profile = _profileService.GetProfile(
            UserId,
            User.FindFirstValue(ClaimTypes.Email) ?? "",
            User.FindFirstValue(ClaimTypes.Name) ?? "");

        try
        {
            var activeChallenge = await _challengeService.GetActiveChallengeForUserAsync(UserId);
            var activeChallenges = activeChallenge != null
                ? new List<Challenge> { activeChallenge }
                : new List<Challenge>();
            ViewBag.FunFact = await _funFactService.GetOrGenerateAsync(profile, activeChallenges, ct);
        }
        catch (Exception ex)
        {
            ViewBag.FunFactError = ex.Message;
            ViewBag.FunFact = new DailyFunFact
            {
                Fact = "Consistency is the most powerful tool in nutrition — even small daily improvements compound over time.",
                Tip = "Log your next meal as soon as you eat it for the most accurate tracking.",
                Emoji = "💡"
            };
        }

        return View(profile);
    }

    [Authorize]
    [HttpPost]
    public IActionResult Save(UserProfile model)
    {
        model.UserId = UserId;
        model.Email = User.FindFirstValue(ClaimTypes.Email) ?? "";

        if (!ProfileService.IsValidUsername(model.Username))
        {
            TempData["Error"] = "Username must be 3–30 characters and contain only letters, numbers, hyphens or underscores.";
            return View("Index", model);
        }

        if (_profileService.IsUsernameTaken(model.Username, UserId))
        {
            TempData["Error"] = $"'{model.Username}' is already taken. Please choose another.";
            return View("Index", model);
        }

        _profileService.SaveProfile(model);
        TempData["Success"] = "Profile saved.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> FunFact(CancellationToken ct)
    {
        var profile = _profileService.GetProfile(
            UserId,
            User.FindFirstValue(ClaimTypes.Email) ?? "",
            User.FindFirstValue(ClaimTypes.Name) ?? "");

        var activeChallenge = await _challengeService.GetActiveChallengeForUserAsync(UserId);
        var activeChallenges = activeChallenge != null
            ? new List<Challenge> { activeChallenge }
            : new List<Challenge>();

        try
        {
            var fact = await _funFactService.GetOrGenerateAsync(profile, activeChallenges, ct);
            return Json(fact);
        }
        catch
        {
            return Json(new DailyFunFact
            {
                Fact = "Consistency is the most powerful tool in nutrition — even small daily improvements compound over time.",
                Tip = "Log your next meal as soon as you eat it for the most accurate tracking.",
                Emoji = "💡"
            });
        }
    }

    [Route("u/{username}")]
    public async Task<IActionResult> Public(string username)
    {
        var profile = _profileService.GetPublicProfileByUsername(username);
        if (profile == null) return NotFound();

        var currentUserId = User.FindFirstValue("dupi:uid");
        if (!string.IsNullOrEmpty(currentUserId) && currentUserId != profile.UserId)
        {
            ViewBag.FriendStatus = await _socialService.GetStatusAsync(currentUserId, profile.UserId);
            ViewBag.CurrentUserId = currentUserId;
            ViewBag.TargetUserId = profile.UserId;
        }

        return View(profile);
    }
}
