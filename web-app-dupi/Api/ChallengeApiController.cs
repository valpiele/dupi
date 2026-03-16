using dupi.Dtos;
using dupi.Models;
using dupi.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace dupi.Api;

[ApiController]
[Route("api/challenges")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class ChallengeApiController : ControllerBase
{
    private readonly ChallengeService _challengeService;
    private readonly SocialService _socialService;

    public ChallengeApiController(ChallengeService challengeService, SocialService socialService)
    {
        _challengeService = challengeService;
        _socialService = socialService;
    }

    private string UserId => User.FindFirstValue("dupi:uid")!;

    [HttpGet]
    public async Task<IActionResult> GetChallenges()
    {
        await _challengeService.TransitionChallengesAsync();
        var vm = await _challengeService.GetUserChallengesAsync(UserId);

        return Ok(new ChallengeIndexDto
        {
            ActiveChallenges = vm.ActiveChallenges.Select(x => MapChallenge(x.Challenge, x.ParticipantCount)).ToList(),
            PendingInvites = vm.PendingInvites.Select(x => MapChallenge(x.Challenge, x.ParticipantCount)).ToList(),
            CompletedChallenges = vm.CompletedChallenges.Select(x => MapChallenge(x.Challenge, x.ParticipantCount)).ToList(),
            CommunityChallenges = vm.CommunityChallenges.Select(x => MapChallenge(x.Challenge, x.ParticipantCount)).ToList()
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ChallengeCreateRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            return BadRequest(new { error = "Title is required." });

        if (!Enum.TryParse<ChallengeMetric>(request.Metric, out var metric))
            return BadRequest(new { error = "Invalid metric." });
        if (!Enum.TryParse<GoalDirection>(request.Direction, out var direction))
            return BadRequest(new { error = "Invalid direction." });
        if (!Enum.TryParse<ChallengeType>(request.Type, out var type))
            return BadRequest(new { error = "Invalid type." });

        var model = new ChallengeCreateViewModel
        {
            Title = request.Title,
            Description = request.Description,
            Metric = metric,
            TargetValue = request.TargetValue,
            Direction = direction,
            Type = type,
            InvitedFriendIds = request.InvitedFriendIds
        };

        var challenge = await _challengeService.CreateAsync(UserId, model);

        if (type == ChallengeType.FriendChallenge && request.InvitedFriendIds.Count > 0)
            await _challengeService.InviteFriendsAsync(challenge.Id, UserId, request.InvitedFriendIds);

        return Ok(MapChallenge(challenge, 1));
    }

    [HttpGet("{id}/leaderboard")]
    public async Task<IActionResult> Leaderboard(int id)
    {
        var leaderboard = await _challengeService.ComputeLeaderboardAsync(id);
        return Ok(leaderboard.Select(p => new LeaderboardEntryDto
        {
            UserId = p.UserId,
            DisplayName = p.Profile?.DisplayName ?? "Unknown",
            Rank = p.Rank,
            DaysHit = p.DaysHit,
            TotalMetricValue = p.TotalMetricValue,
            AverageMetricValue = p.AverageMetricValue,
            AverageScore = p.AverageScore,
            TotalMeals = p.TotalMeals,
            DailyBreakdown = p.DailyBreakdown.Select(d => new DayProgressDto
            {
                Date = d.Date,
                MetricValue = d.MetricValue,
                MealCount = d.MealCount,
                TargetHit = d.TargetHit
            }).ToList()
        }));
    }

    [HttpPost("{id}/join")]
    public async Task<IActionResult> Join(int id)
    {
        var joined = await _challengeService.JoinAsync(id, UserId);
        return joined ? Ok(new { joined = true }) : BadRequest(new { error = "Cannot join this challenge." });
    }

    [HttpPost("{id}/leave")]
    public async Task<IActionResult> Leave(int id)
    {
        var left = await _challengeService.LeaveAsync(id, UserId);
        return left ? Ok(new { left = true }) : BadRequest(new { error = "Cannot leave this challenge." });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _challengeService.DeleteAsync(id, UserId);
        return deleted ? NoContent() : BadRequest(new { error = "Cannot delete this challenge." });
    }

    private static ChallengeDto MapChallenge(Challenge c, int participantCount) => new()
    {
        Id = c.Id,
        CreatorId = c.CreatorId,
        Title = c.Title,
        Description = c.Description,
        Metric = c.Metric.ToString(),
        TargetValue = c.TargetValue,
        Direction = c.Direction.ToString(),
        Type = c.Type.ToString(),
        Status = c.Status.ToString(),
        StartDate = c.StartDate,
        EndDate = c.EndDate,
        ParticipantCount = participantCount
    };
}
