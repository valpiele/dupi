namespace dupi.Models;

public enum ChallengeType { FriendChallenge, Community }
public enum ChallengeStatus { Pending, Active, Completed }

public class Challenge
{
    public int Id { get; set; }
    public string CreatorId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int ProteinTargetGrams { get; set; }
    public ChallengeType Type { get; set; }
    public ChallengeStatus Status { get; set; } = ChallengeStatus.Pending;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
