namespace dupi.Dtos;

public class ChallengeDto
{
    public int Id { get; set; }
    public string CreatorId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Metric { get; set; } = string.Empty;
    public double TargetValue { get; set; }
    public string Direction { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int ParticipantCount { get; set; }
}

public class ChallengeCreateRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Metric { get; set; } = "Protein";
    public double TargetValue { get; set; } = 120;
    public string Direction { get; set; } = "AtLeast";
    public string Type { get; set; } = "FriendChallenge";
    public List<string> InvitedFriendIds { get; set; } = new();
}

public class ChallengeIndexDto
{
    public List<ChallengeDto> ActiveChallenges { get; set; } = new();
    public List<ChallengeDto> PendingInvites { get; set; } = new();
    public List<ChallengeDto> CompletedChallenges { get; set; } = new();
    public List<ChallengeDto> CommunityChallenges { get; set; } = new();
}

public class LeaderboardEntryDto
{
    public string UserId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public int Rank { get; set; }
    public int DaysHit { get; set; }
    public double TotalMetricValue { get; set; }
    public double AverageMetricValue { get; set; }
    public double AverageScore { get; set; }
    public int TotalMeals { get; set; }
    public List<DayProgressDto> DailyBreakdown { get; set; } = new();
}

public class DayProgressDto
{
    public DateTime Date { get; set; }
    public double MetricValue { get; set; }
    public int MealCount { get; set; }
    public bool TargetHit { get; set; }
}
