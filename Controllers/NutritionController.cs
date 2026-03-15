using dupi.Models;
using dupi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

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

    // POST /Nutrition/AnalyzeStream — SSE endpoint for streaming Gemini thinking + result
    [HttpPost("Nutrition/AnalyzeStream"), ValidateAntiForgeryToken]
    public async Task AnalyzeStream(string? title, string? description, IFormFile? file)
    {
        Response.ContentType = "text/event-stream";
        Response.Headers["Cache-Control"] = "no-cache";
        Response.Headers["X-Accel-Buffering"] = "no";

        async Task Send(object payload)
        {
            await Response.WriteAsync($"data: {JsonSerializer.Serialize(payload)}\n\n");
            await Response.Body.FlushAsync();
        }

        if (file == null && string.IsNullOrWhiteSpace(description))
        {
            await Send(new { type = "error", message = "Please upload a file or enter a description of your meal." });
            return;
        }

        byte[]? fileData = null;
        string? mimeType = null;
        string? extension = null;

        if (file != null && file.Length > 0)
        {
            if (file.Length > 10 * 1024 * 1024)
            {
                await Send(new { type = "error", message = "File must be under 10MB." });
                return;
            }

            mimeType = file.ContentType.ToLower();
            if (!AllowedMimeTypes.Contains(mimeType))
            {
                await Send(new { type = "error", message = "Only images (JPEG, PNG, WebP) and PDFs are accepted." });
                return;
            }

            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            fileData = ms.ToArray();
            extension = Path.GetExtension(file.FileName).ToLower();
        }

        var outputBuffer = new StringBuilder();
        try
        {
            await foreach (var (isThinking, text) in _geminiService.StreamAnalyzeNutritionAsync(
                description, fileData, mimeType, HttpContext.RequestAborted))
            {
                if (isThinking)
                    await Send(new { type = "thinking", text });
                else
                    outputBuffer.Append(text);
            }
        }
        catch (OperationCanceledException)
        {
            return;
        }
        catch (Exception ex)
        {
            await Send(new { type = "error", message = $"Could not analyse your plan right now: {ex.Message}" });
            return;
        }

        NutritionAnalysis analysis;
        try
        {
            analysis = JsonSerializer.Deserialize<NutritionAnalysis>(outputBuffer.ToString())
                ?? throw new InvalidOperationException("Empty response from Gemini.");
        }
        catch (Exception ex)
        {
            await Send(new { type = "error", message = $"Could not parse result: {ex.Message}" });
            return;
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

        await Send(new { type = "done", redirectUrl = Url.Action(nameof(Result), new { id = plan.Id }) });
    }

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
