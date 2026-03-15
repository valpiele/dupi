using dupi.Models;
using dupi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace dupi.Controllers;

public class ProfileController : Controller
{
    private readonly ProfileService _profileService;

    public ProfileController(ProfileService profileService)
    {
        _profileService = profileService;
    }

    private string UserId => User.FindFirstValue("dupi:uid")!;

    [Authorize]
    public IActionResult Index()
    {
        var profile = _profileService.GetProfile(
            UserId,
            User.FindFirstValue(ClaimTypes.Email) ?? "",
            User.FindFirstValue(ClaimTypes.Name) ?? "");

        ViewBag.Projects = _profileService.GetProjects(UserId);
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
            ViewBag.Projects = _profileService.GetProjects(UserId);
            return View("Index", model);
        }

        if (_profileService.IsUsernameTaken(model.Username, UserId))
        {
            TempData["Error"] = $"'{model.Username}' is already taken. Please choose another.";
            ViewBag.Projects = _profileService.GetProjects(UserId);
            return View("Index", model);
        }

        _profileService.SaveProfile(model);
        TempData["Success"] = "Profile saved.";
        return RedirectToAction(nameof(Index));
    }

    [Route("u/{username}")]
    public IActionResult Public(string username)
    {
        var profile = _profileService.GetPublicProfileByUsername(username);
        if (profile == null) return NotFound();

        ViewBag.Projects = _profileService.GetProjects(profile.UserId);
        return View(profile);
    }

    [Route("u/{username}/view")]
    public IActionResult PublicView(string username, string fileName)
    {
        var profile = _profileService.GetPublicProfileByUsername(username);
        if (profile == null) return NotFound();

        if (!_profileService.ProjectExists(profile.UserId, fileName)) return NotFound();
        var stream = _profileService.DownloadProject(profile.UserId, fileName);
        Response.Headers["Content-Disposition"] = $"inline; filename=\"{fileName}\"";
        return File(stream, "application/pdf");
    }
}
