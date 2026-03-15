using dupi.Models;
using dupi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace dupi.Controllers;

[Authorize]
public class NutritionController : Controller
{
    private readonly NutritionService _nutritionService;
    private readonly GeminiService _geminiService;

    private static readonly string[] AllowedMimeTypes =
        ["image/jpeg", "image/png", "image/webp", "application/pdf"];

    public NutritionController(NutritionService nutritionService, GeminiService geminiService)
    {
        _nutritionService = nutritionService;
        _geminiService = geminiService;
    }

    private string UserId => User.FindFirstValue("dupi:uid")!;

    // GET /Nutrition
    public IActionResult Index()
    {
        var plans = _nutritionService.GetPlans(UserId);
        return View(plans);
    }

    // GET /Nutrition/Create
    [HttpGet]
    public IActionResult Create() => View();

    // POST /Nutrition/Create
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string? title, string? description, IFormFile? file)
    {
        if (file == null && string.IsNullOrWhiteSpace(description))
        {
            TempData["Error"] = "Please upload a file or enter a description of your meal.";
            return View();
        }

        byte[]? fileData = null;
        string? mimeType = null;
        string? extension = null;

        if (file != null && file.Length > 0)
        {
            if (file.Length > 10 * 1024 * 1024)
            {
                TempData["Error"] = "File must be under 10MB.";
                return View();
            }

            mimeType = file.ContentType.ToLower();
            if (!AllowedMimeTypes.Contains(mimeType))
            {
                TempData["Error"] = "Only images (JPEG, PNG, WebP) and PDFs are accepted.";
                return View();
            }

            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            fileData = ms.ToArray();
            extension = Path.GetExtension(file.FileName).ToLower();
        }

        dupi.Models.NutritionAnalysis analysis;
        try
        {
            analysis = await _geminiService.AnalyzeNutritionAsync(description, fileData, mimeType);
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Could not analyse your plan right now: {ex.Message}";
            return View();
        }

        var plan = new NutritionPlan
        {
            UserId = UserId,
            Title = string.IsNullOrWhiteSpace(title) ? "Nutrition Plan" : title.Trim(),
            InputType = fileData != null ? (mimeType!.StartsWith("image") ? "image" : "pdf") : "text",
            OriginalFileName = file?.FileName,
            HasFile = fileData != null,
            FileExtension = extension,
            FoodDescription = analysis.FoodDescription,
            CaloriesMin = analysis.CaloriesMin,
            CaloriesMax = analysis.CaloriesMax,
            Proteins = analysis.Proteins,
            Carbohydrates = analysis.Carbohydrates,
            Fats = analysis.Fats,
            WhatsGood = analysis.WhatsGood,
            WhatToImprove = analysis.WhatToImprove,
            Score = analysis.Score,
            ScoreSummary = analysis.ScoreSummary
        };

        _nutritionService.SavePlan(plan);

        if (fileData != null && extension != null)
        {
            using var ms = new MemoryStream(fileData);
            await _nutritionService.SaveFileAsync(UserId, plan.Id, extension, ms);
        }

        return RedirectToAction(nameof(Result), new { id = plan.Id });
    }

    // GET /Nutrition/Result/{id}
    public IActionResult Result(string id)
    {
        var plan = _nutritionService.GetPlan(UserId, id);
        if (plan == null) return NotFound();
        return View(plan);
    }

    // GET /Nutrition/File/{id}
    public IActionResult File(string id)
    {
        var plan = _nutritionService.GetPlan(UserId, id);
        if (plan == null || !plan.HasFile || plan.FileExtension == null) return NotFound();

        var stream = _nutritionService.GetFile(UserId, id, plan.FileExtension);
        if (stream == null) return NotFound();

        var mime = plan.InputType == "pdf" ? "application/pdf" : $"image/{plan.FileExtension.TrimStart('.')}";
        return File(stream, mime);
    }

    // POST /Nutrition/Delete
    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Delete(string id)
    {
        var plan = _nutritionService.GetPlan(UserId, id);
        if (plan != null)
            _nutritionService.DeletePlan(UserId, id, plan.FileExtension);
        return RedirectToAction(nameof(Index));
    }
}
