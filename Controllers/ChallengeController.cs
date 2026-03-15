using dupi.Models;
using dupi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using dupi.Hubs;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace dupi.Controllers;

[Authorize]
public class ChallengeController : Controller
{
    private readonly ChallengeService _challengeService;
    private readonly SocialService _socialService;
    private readonly GeminiService _geminiService;

    public ChallengeController(
        ChallengeService challengeService,
        SocialService socialService,
        GeminiService geminiService)
    {
        _challengeService = challengeService;
        _socialService = socialService;
        _geminiService = geminiService;
    }

    private string UserId => User.FindFirstValue("dupi:uid")!;

    // GET /Challenge
    public async Task<IActionResult> Index()
    {
        await _challengeService.TransitionChallengesAsync();
        var vm = await _challengeService.GetUserChallengesAsync(UserId);
        return View(vm);
    }

    // GET /Challenge/Create
    [HttpGet]
    public async Task<IActionResult> Create(string? friendId)
    {
        var friends = await _socialService.GetFriendsAsync(UserId);
        ViewBag.Friends = friends;
        var vm = new ChallengeCreateViewModel();
        if (!string.IsNullOrEmpty(friendId))
        {
            vm.InvitedFriendIds = new List<string> { friendId };
            vm.Type = ChallengeType.FriendChallenge;
        }
        return View(vm);
    }

    // POST /Challenge/Create
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ChallengeCreateViewModel model)
    {
        if (string.IsNullOrWhiteSpace(model.Title))
        {
            TempData["Error"] = "Please enter a challenge title.";
            var friends = await _socialService.GetFriendsAsync(UserId);
            ViewBag.Friends = friends;
            return View(model);
        }

        var challenge = await _challengeService.CreateAsync(UserId, model);

        if (model.Type == ChallengeType.FriendChallenge && model.InvitedFriendIds.Count > 0)
        {
            await _challengeService.InviteFriendsAsync(challenge.Id, UserId, model.InvitedFriendIds);
        }

        return RedirectToAction(nameof(Dashboard), new { id = challenge.Id });
    }

    // GET /Challenge/Dashboard/{id}
    public async Task<IActionResult> Dashboard(int id)
    {
        await _challengeService.TransitionChallengesAsync();

        var challenge = await _challengeService.GetAsync(id);
        if (challenge == null) return NotFound();

        var leaderboard = await _challengeService.ComputeLeaderboardAsync(id);
        var isParticipant = await _challengeService.IsParticipantAsync(id, UserId);

        var vm = new ChallengeDashboardViewModel
        {
            Challenge = challenge,
            Leaderboard = leaderboard,
            MyProgress = leaderboard.FirstOrDefault(p => p.UserId == UserId),
            IsParticipant = isParticipant,
            IsCreator = challenge.CreatorId == UserId
        };

        return View(vm);
    }

    // POST /Challenge/Join/{id}
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Join(int id)
    {
        await _challengeService.JoinAsync(id, UserId);
        return RedirectToAction(nameof(Dashboard), new { id });
    }

    // POST /Challenge/Leave/{id}
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Leave(int id)
    {
        await _challengeService.LeaveAsync(id, UserId);
        return RedirectToAction(nameof(Index));
    }

    // POST /Challenge/Delete/{id}
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        await _challengeService.DeleteAsync(id, UserId);
        return RedirectToAction(nameof(Index));
    }

    // GET /Challenge/Summary/{id}
    public async Task<IActionResult> Summary(int id)
    {
        await _challengeService.TransitionChallengesAsync();
        var challenge = await _challengeService.GetAsync(id);
        if (challenge == null) return NotFound();
        if (challenge.Status != ChallengeStatus.Completed) return RedirectToAction(nameof(Dashboard), new { id });

        var isParticipant = await _challengeService.IsParticipantAsync(id, UserId);
        if (!isParticipant) return Forbid();

        ViewBag.Challenge = challenge;
        return View();
    }

    // POST /Challenge/SummaryStream/{id}
    [HttpPost("Challenge/SummaryStream/{id}"), ValidateAntiForgeryToken]
    public async Task SummaryStream(int id)
    {
        Response.ContentType = "text/event-stream";
        Response.Headers["Cache-Control"] = "no-cache";
        Response.Headers["X-Accel-Buffering"] = "no";

        async Task Send(object payload)
        {
            await Response.WriteAsync($"data: {JsonSerializer.Serialize(payload)}\n\n");
            await Response.Body.FlushAsync();
        }

        var challenge = await _challengeService.GetAsync(id);
        if (challenge == null || challenge.Status != ChallengeStatus.Completed)
        {
            await Send(new { type = "error", message = "Challenge not found or not completed." });
            return;
        }

        var summaryText = await _challengeService.BuildChallengeSummaryTextAsync(id);

        var outputBuffer = new StringBuilder();
        try
        {
            await foreach (var (isThinking, text) in _geminiService.StreamChallengeSummaryAsync(
                summaryText, HttpContext.RequestAborted))
            {
                if (isThinking)
                    await Send(new { type = "thinking", text });
                else
                {
                    outputBuffer.Append(text);
                    await Send(new { type = "output", text });
                }
            }
        }
        catch (OperationCanceledException) { return; }
        catch (Exception ex)
        {
            await Send(new { type = "error", message = $"Could not generate summary: {ex.Message}" });
            return;
        }

        ChallengeSummary summary;
        try
        {
            summary = JsonSerializer.Deserialize<ChallengeSummary>(outputBuffer.ToString(),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                ?? throw new InvalidOperationException("Empty response from Gemini.");
        }
        catch (Exception ex)
        {
            await Send(new { type = "error", message = $"Could not parse summary: {ex.Message}" });
            return;
        }

        await Send(new { type = "done", summary });
    }

    // GET /Challenge/LeaderboardData/{id}
    [HttpGet]
    public async Task<IActionResult> LeaderboardData(int id)
    {
        var leaderboard = await _challengeService.ComputeLeaderboardAsync(id);
        var data = leaderboard.Select(p => new
        {
            userId = p.UserId,
            displayName = p.Profile?.DisplayName ?? "Unknown",
            rank = p.Rank,
            daysHit = p.DaysHit,
            totalMetricValue = p.TotalMetricValue,
            averageMetricValue = p.AverageMetricValue,
            totalMeals = p.TotalMeals,
            dailyBreakdown = p.DailyBreakdown.Select(d => new
            {
                date = d.Date.ToString("o"),
                metricValue = d.MetricValue,
                mealCount = d.MealCount,
                targetHit = d.TargetHit
            })
        });
        return Json(data);
    }
}
