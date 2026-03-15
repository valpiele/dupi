namespace dupi.Models;

public class ChallengeCreateViewModel
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int ProteinTargetGrams { get; set; } = 120;
    public ChallengeType Type { get; set; } = ChallengeType.FriendChallenge;
    public List<string> InvitedFriendIds { get; set; } = new();
}

public class ChallengeIndexViewModel
{
    public List<(Challenge Challenge, int ParticipantCount, int? MyRank, int? MyDaysHit)> ActiveChallenges { get; set; } = new();
    public List<(Challenge Challenge, int ParticipantCount)> PendingInvites { get; set; } = new();
    public List<(Challenge Challenge, int ParticipantCount)> CompletedChallenges { get; set; } = new();
    public List<(Challenge Challenge, int ParticipantCount)> CommunityChallenges { get; set; } = new();
}

public class ChallengeDashboardViewModel
{
    public Challenge Challenge { get; set; } = null!;
    public List<ParticipantProgress> Leaderboard { get; set; } = new();
    public ParticipantProgress? MyProgress { get; set; }
    public bool IsParticipant { get; set; }
    public bool IsCreator { get; set; }
    public UserProfile? CreatorProfile { get; set; }
}

public class ParticipantProgress
{
    public string UserId { get; set; } = string.Empty;
    public UserProfile? Profile { get; set; }
    public int DaysHit { get; set; }
    public double TotalProtein { get; set; }
    public double AverageProtein { get; set; }
    public double AverageScore { get; set; }
    public int TotalMeals { get; set; }
    public List<DayProgress> DailyBreakdown { get; set; } = new();
    public int Rank { get; set; }
}

public class DayProgress
{
    public DateTime Date { get; set; }
    public double ProteinGrams { get; set; }
    public int MealCount { get; set; }
    public double AverageScore { get; set; }
    public bool TargetHit { get; set; }
}

public class ChallengeSummary
{
    public string WinnerAnalysis { get; set; } = string.Empty;
    public List<string> Highlights { get; set; } = new();
    public List<string> ImprovementTips { get; set; } = new();
    public List<string> FunStats { get; set; } = new();
}
