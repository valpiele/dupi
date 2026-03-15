using dupi.Data;
using dupi.Models;
using Microsoft.EntityFrameworkCore;

namespace dupi.Services;

public class ChallengeService
{
    private readonly ApplicationDbContext _db;
    private readonly ProfileService _profileService;

    public ChallengeService(ApplicationDbContext db, ProfileService profileService)
    {
        _db = db;
        _profileService = profileService;
    }

    // ── Lifecycle transitions (lazy, no background jobs) ──────────────────

    public async Task TransitionChallengesAsync()
    {
        var now = DateTime.UtcNow;
        await _db.Challenges
            .Where(c => c.Status == ChallengeStatus.Pending && c.StartDate <= now)
            .ExecuteUpdateAsync(s => s.SetProperty(c => c.Status, ChallengeStatus.Active));
        await _db.Challenges
            .Where(c => c.Status == ChallengeStatus.Active && c.EndDate <= now)
            .ExecuteUpdateAsync(s => s.SetProperty(c => c.Status, ChallengeStatus.Completed));
    }

    // ── CRUD ──────────────────────────────────────────────────────────────

    public async Task<Challenge> CreateAsync(string creatorId, ChallengeCreateViewModel model)
    {
        var startDate = DateTime.UtcNow.Date.AddDays(1); // tomorrow 00:00 UTC
        var challenge = new Challenge
        {
            CreatorId = creatorId,
            Title = model.Title.Trim(),
            Description = model.Description?.Trim(),
            ProteinTargetGrams = Math.Clamp(model.ProteinTargetGrams, 10, 300),
            Type = model.Type,
            Status = model.Type == ChallengeType.Community ? ChallengeStatus.Active : ChallengeStatus.Pending,
            StartDate = model.Type == ChallengeType.Community ? DateTime.UtcNow.Date : startDate,
            EndDate = model.Type == ChallengeType.Community ? DateTime.UtcNow.Date.AddDays(7) : startDate.AddDays(7)
        };

        _db.Challenges.Add(challenge);
        await _db.SaveChangesAsync();

        // Add creator as participant
        _db.ChallengeParticipants.Add(new ChallengeParticipant
        {
            ChallengeId = challenge.Id,
            UserId = creatorId
        });
        await _db.SaveChangesAsync();

        return challenge;
    }

    public Task<Challenge?> GetAsync(int id) =>
        _db.Challenges.FirstOrDefaultAsync(c => c.Id == id);

    public async Task<ChallengeIndexViewModel> GetUserChallengesAsync(string userId)
    {
        var participantChallengeIds = await _db.ChallengeParticipants
            .Where(cp => cp.UserId == userId)
            .Select(cp => cp.ChallengeId)
            .ToListAsync();

        var myChallenges = await _db.Challenges
            .Where(c => participantChallengeIds.Contains(c.Id))
            .ToListAsync();

        var communityChallenges = await _db.Challenges
            .Where(c => c.Type == ChallengeType.Community &&
                        c.Status == ChallengeStatus.Active &&
                        !participantChallengeIds.Contains(c.Id))
            .OrderByDescending(c => c.CreatedAt)
            .Take(20)
            .ToListAsync();

        var allChallengeIds = myChallenges.Select(c => c.Id)
            .Concat(communityChallenges.Select(c => c.Id))
            .Distinct().ToList();

        var counts = await _db.ChallengeParticipants
            .Where(cp => allChallengeIds.Contains(cp.ChallengeId))
            .GroupBy(cp => cp.ChallengeId)
            .Select(g => new { ChallengeId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ChallengeId, x => x.Count);

        int GetCount(int id) => counts.GetValueOrDefault(id, 0);

        return new ChallengeIndexViewModel
        {
            ActiveChallenges = myChallenges
                .Where(c => c.Status == ChallengeStatus.Active)
                .OrderByDescending(c => c.StartDate)
                .Select(c => (c, GetCount(c.Id), (int?)null, (int?)null))
                .ToList(),
            PendingInvites = myChallenges
                .Where(c => c.Status == ChallengeStatus.Pending && c.CreatorId != userId)
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => (c, GetCount(c.Id)))
                .ToList(),
            CompletedChallenges = myChallenges
                .Where(c => c.Status == ChallengeStatus.Completed)
                .OrderByDescending(c => c.EndDate)
                .Select(c => (c, GetCount(c.Id)))
                .ToList(),
            CommunityChallenges = communityChallenges
                .Select(c => (c, GetCount(c.Id)))
                .ToList()
        };
    }

    public async Task<bool> JoinAsync(int challengeId, string userId)
    {
        var challenge = await _db.Challenges.FindAsync(challengeId);
        if (challenge == null || challenge.EndDate <= DateTime.UtcNow) return false;

        var exists = await _db.ChallengeParticipants
            .AnyAsync(cp => cp.ChallengeId == challengeId && cp.UserId == userId);
        if (exists) return false;

        _db.ChallengeParticipants.Add(new ChallengeParticipant
        {
            ChallengeId = challengeId,
            UserId = userId
        });
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> LeaveAsync(int challengeId, string userId)
    {
        var challenge = await _db.Challenges.FindAsync(challengeId);
        if (challenge == null || challenge.Status != ChallengeStatus.Pending) return false;

        var participant = await _db.ChallengeParticipants
            .FirstOrDefaultAsync(cp => cp.ChallengeId == challengeId && cp.UserId == userId);
        if (participant == null) return false;

        _db.ChallengeParticipants.Remove(participant);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(int challengeId, string userId)
    {
        var challenge = await _db.Challenges.FindAsync(challengeId);
        if (challenge == null || challenge.CreatorId != userId || challenge.Status != ChallengeStatus.Pending)
            return false;

        _db.Challenges.Remove(challenge);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task InviteFriendsAsync(int challengeId, string creatorId, List<string> friendIds)
    {
        var challenge = await _db.Challenges.FindAsync(challengeId);
        if (challenge == null || challenge.CreatorId != creatorId) return;

        var existing = await _db.ChallengeParticipants
            .Where(cp => cp.ChallengeId == challengeId)
            .Select(cp => cp.UserId)
            .ToListAsync();

        var toAdd = friendIds.Where(id => !existing.Contains(id)).ToList();
        foreach (var friendId in toAdd)
        {
            _db.ChallengeParticipants.Add(new ChallengeParticipant
            {
                ChallengeId = challengeId,
                UserId = friendId
            });
        }
        await _db.SaveChangesAsync();
    }

    public Task<bool> IsParticipantAsync(int challengeId, string userId) =>
        _db.ChallengeParticipants.AnyAsync(cp => cp.ChallengeId == challengeId && cp.UserId == userId);

    public async Task<Challenge?> GetActiveChallengeForUserAsync(string userId)
    {
        var participantChallengeIds = await _db.ChallengeParticipants
            .Where(cp => cp.UserId == userId)
            .Select(cp => cp.ChallengeId)
            .ToListAsync();

        return await _db.Challenges
            .Where(c => participantChallengeIds.Contains(c.Id) && c.Status == ChallengeStatus.Active)
            .OrderByDescending(c => c.StartDate)
            .FirstOrDefaultAsync();
    }

    // ── Leaderboard computation ──────────────────────────────────────────

    public async Task<List<ParticipantProgress>> ComputeLeaderboardAsync(int challengeId)
    {
        var challenge = await _db.Challenges.FindAsync(challengeId);
        if (challenge == null) return new();

        var participants = await _db.ChallengeParticipants
            .Where(cp => cp.ChallengeId == challengeId)
            .ToListAsync();

        var userIds = participants.Select(p => p.UserId).ToList();

        var plans = await _db.NutritionPlans
            .Where(p => userIds.Contains(p.UserId) &&
                        p.CreatedAt >= challenge.StartDate &&
                        p.CreatedAt < challenge.EndDate &&
                        p.CaloriesMin > 0)
            .ToListAsync();

        var plansByUser = plans.GroupBy(p => p.UserId).ToDictionary(g => g.Key, g => g.ToList());

        var results = new List<ParticipantProgress>();
        var today = DateTime.UtcNow.Date;

        foreach (var participant in participants)
        {
            var userPlans = plansByUser.GetValueOrDefault(participant.UserId, new());
            var dailyBreakdown = new List<DayProgress>();

            for (int day = 0; day < 7; day++)
            {
                var date = challenge.StartDate.Date.AddDays(day);
                var dayPlans = userPlans.Where(p => p.CreatedAt.Date == date).ToList();
                var dayProtein = dayPlans.Sum(p => p.Proteins);
                var dayScores = dayPlans.Where(p => p.Score > 0).ToList();

                dailyBreakdown.Add(new DayProgress
                {
                    Date = date,
                    ProteinGrams = dayProtein,
                    MealCount = dayPlans.Count,
                    AverageScore = dayScores.Count > 0 ? dayScores.Average(p => p.Score) : 0,
                    TargetHit = dayProtein >= challenge.ProteinTargetGrams
                });
            }

            var scoredPlans = userPlans.Where(p => p.Score > 0).ToList();
            var totalProtein = userPlans.Sum(p => p.Proteins);
            var activeDays = dailyBreakdown.Where(d => d.Date <= today).ToList();
            var daysWithData = activeDays.Count > 0 ? activeDays.Count : 1;

            results.Add(new ParticipantProgress
            {
                UserId = participant.UserId,
                Profile = _profileService.GetProfileByUserId(participant.UserId),
                DaysHit = dailyBreakdown.Count(d => d.TargetHit && d.Date <= today),
                TotalProtein = totalProtein,
                AverageProtein = totalProtein / daysWithData,
                AverageScore = scoredPlans.Count > 0 ? scoredPlans.Average(p => p.Score) : 0,
                TotalMeals = userPlans.Count,
                DailyBreakdown = dailyBreakdown
            });
        }

        // Rank: DaysHit DESC → AverageProtein DESC → AverageScore DESC
        var ranked = results
            .OrderByDescending(p => p.DaysHit)
            .ThenByDescending(p => p.AverageProtein)
            .ThenByDescending(p => p.AverageScore)
            .ToList();

        for (int i = 0; i < ranked.Count; i++)
            ranked[i].Rank = i + 1;

        return ranked;
    }

    // ── Challenge summary for Gemini ────────────────────────────────────

    public async Task<string> BuildChallengeSummaryTextAsync(int challengeId)
    {
        var challenge = await _db.Challenges.FindAsync(challengeId);
        if (challenge == null) return string.Empty;

        var leaderboard = await ComputeLeaderboardAsync(challengeId);
        var sb = new System.Text.StringBuilder();

        sb.AppendLine($"Challenge: {challenge.Title}");
        sb.AppendLine($"Daily protein target: {challenge.ProteinTargetGrams}g");
        sb.AppendLine($"Duration: {challenge.StartDate:MMM d} – {challenge.EndDate:MMM d, yyyy}");
        sb.AppendLine($"Participants: {leaderboard.Count}");
        sb.AppendLine();

        foreach (var p in leaderboard)
        {
            var name = p.Profile?.DisplayName ?? "Unknown";
            sb.AppendLine($"--- {name} (Rank #{p.Rank}) ---");
            sb.AppendLine($"  Days hitting target: {p.DaysHit}/7");
            sb.AppendLine($"  Total protein: {p.TotalProtein:0}g");
            sb.AppendLine($"  Average daily protein: {p.AverageProtein:0}g");
            sb.AppendLine($"  Average meal score: {p.AverageScore:0.1}/10");
            sb.AppendLine($"  Total meals logged: {p.TotalMeals}");

            var bestDay = p.DailyBreakdown.OrderByDescending(d => d.ProteinGrams).FirstOrDefault();
            var worstDay = p.DailyBreakdown.Where(d => d.MealCount > 0).OrderBy(d => d.ProteinGrams).FirstOrDefault();
            if (bestDay != null)
                sb.AppendLine($"  Best day: {bestDay.Date:MMM d} with {bestDay.ProteinGrams:0}g protein");
            if (worstDay != null)
                sb.AppendLine($"  Worst day: {worstDay.Date:MMM d} with {worstDay.ProteinGrams:0}g protein");
            sb.AppendLine();
        }

        var totalMeals = leaderboard.Sum(p => p.TotalMeals);
        var totalProteinAll = leaderboard.Sum(p => p.TotalProtein);
        sb.AppendLine($"Overall: {totalMeals} meals logged, {totalProteinAll:0}g total protein consumed by all participants.");

        return sb.ToString();
    }

    public async Task<int> GetPendingInviteCountAsync(string userId)
    {
        var participantChallengeIds = await _db.ChallengeParticipants
            .Where(cp => cp.UserId == userId)
            .Select(cp => cp.ChallengeId)
            .ToListAsync();

        return await _db.Challenges
            .CountAsync(c => participantChallengeIds.Contains(c.Id) &&
                             c.Status == ChallengeStatus.Pending &&
                             c.CreatorId != userId);
    }
}
