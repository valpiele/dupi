using System.Text.Json.Serialization;

namespace dupi.Models;

public class DailyFunFact
{
    [JsonPropertyName("fact")]
    public string Fact { get; set; } = string.Empty;

    [JsonPropertyName("tip")]
    public string Tip { get; set; } = string.Empty;

    [JsonPropertyName("emoji")]
    public string Emoji { get; set; } = "💡";
}
