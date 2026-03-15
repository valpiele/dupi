using dupi.Models;
using dupi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace dupi.Controllers;

[Authorize]
public class ProjectsController : Controller
{
    private readonly ProfileService _profileService;

    public ProjectsController(ProfileService profileService)
    {
        _profileService = profileService;
    }

    private string UserId => User.FindFirstValue("dupi:uid")!;

    public IActionResult Index()
    {
        var projects = _profileService.GetProjects(UserId);
        return View(projects);
    }

    [HttpPost]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            TempData["Error"] = "Please select a PDF file.";
            return RedirectToAction(nameof(Index));
        }

        if (!file.ContentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase)
            && !file.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
        {
            TempData["Error"] = "Only PDF files are allowed.";
            return RedirectToAction(nameof(Index));
        }

        using var stream = file.OpenReadStream();
        await _profileService.UploadProjectAsync(UserId, Path.GetFileName(file.FileName), stream);

        TempData["Success"] = $"'{Path.GetFileNameWithoutExtension(file.FileName)}' uploaded successfully.";
        return RedirectToAction(nameof(Index));
    }

    public new IActionResult View(string fileName)
    {
        if (!_profileService.ProjectExists(UserId, fileName)) return NotFound();
        var stream = _profileService.DownloadProject(UserId, fileName);
        Response.Headers["Content-Disposition"] = $"inline; filename=\"{fileName}\"";
        return File(stream, "application/pdf");
    }

    public IActionResult Download(string fileName)
    {
        if (!_profileService.ProjectExists(UserId, fileName)) return NotFound();
        var stream = _profileService.DownloadProject(UserId, fileName);
        return File(stream, "application/pdf", fileName);
    }

    [HttpPost]
    public IActionResult Delete(string fileName)
    {
        _profileService.DeleteProject(UserId, fileName);
        TempData["Success"] = $"'{Path.GetFileNameWithoutExtension(fileName)}' deleted.";
        return RedirectToAction(nameof(Index));
    }
}
