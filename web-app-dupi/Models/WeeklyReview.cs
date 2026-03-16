using System.Text.Json.Serialization;

namespace dupi.Models;

public class WeeklyReview
{
    [JsonPropertyName("overall_score")]
    public int OverallScore { get; set; }

    [JsonPropertyName("overall_summary")]
    public string OverallSummary { get; set; } = string.Empty;

    [JsonPropertyName("went_well")]
    public List<string> WentWell { get; set; } = new();

    [JsonPropertyName("to_improve")]
    public List<string> ToImprove { get; set; } = new();

    [JsonPropertyName("next_week_goals")]
    public List<string> NextWeekGoals { get; set; } = new();
}
