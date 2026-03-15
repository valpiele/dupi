namespace dupi.Models;

public class ChallengeParticipant
{
    public int Id { get; set; }
    public int ChallengeId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
}
