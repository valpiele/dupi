using dupi.Models;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace dupi.Services;

public class GeminiService
{
    private readonly HttpClient _http;
    private readonly string _apiKey;
    private const string StreamEndpoint = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:streamGenerateContent?alt=sse&key={0}";

    public GeminiService(HttpClient http, IConfiguration config)
    {
        _http = http;
        _apiKey = config["Gemini:ApiKey"]
            ?? throw new InvalidOperationException("Gemini:ApiKey is not configured.");
    }

    public async IAsyncEnumerable<(bool IsThinking, string Text)> StreamAnalyzeNutritionAsync(
        string? userText, byte[]? fileData, string? mimeType, string? contextSummary = null,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var url = string.Format(StreamEndpoint, _apiKey);
        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(BuildBody(userText, fileData, mimeType, contextSummary, withThinking: true))
        };

        var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException($"Gemini API error {(int)response.StatusCode}: {error}");
        }

        using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream && !ct.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(ct);
            if (line == null || !line.StartsWith("data: ")) continue;

            JsonElement chunk;
            try { chunk = JsonSerializer.Deserialize<JsonElement>(line["data: ".Length..]); }
            catch { continue; }

            if (!chunk.TryGetProperty("candidates", out var candidates) || candidates.GetArrayLength() == 0) continue;
            if (!candidates[0].TryGetProperty("content", out var content)) continue;
            if (!content.TryGetProperty("parts", out var parts)) continue;

            foreach (var part in parts.EnumerateArray())
            {
                if (!part.TryGetProperty("text", out var textEl)) continue;
                var text = textEl.GetString() ?? "";
                if (string.IsNullOrEmpty(text)) continue;

                bool isThought = part.TryGetProperty("thought", out var thoughtEl) && thoughtEl.GetBoolean();
                yield return (isThought, text);
            }
        }
    }

    public async IAsyncEnumerable<(bool IsThinking, string Text)> StreamWeeklyReviewAsync(
        string weekSummary,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var schema = new
        {
            type = "OBJECT",
            properties = new
            {
                overall_score   = new { type = "INTEGER", description = "Overall nutrition score for the week, 1-10" },
                overall_summary = new { type = "STRING",  description = "One sentence summarising the week's nutrition" },
                went_well       = new { type = "ARRAY", items = new { type = "STRING" }, description = "3 things the user did well this week" },
                to_improve      = new { type = "ARRAY", items = new { type = "STRING" }, description = "3 specific areas to improve" },
                next_week_goals = new { type = "ARRAY", items = new { type = "STRING" }, description = "3 concrete, actionable goals for next week" }
            },
            required = new[] { "overall_score", "overall_summary", "went_well", "to_improve", "next_week_goals" }
        };

        var prompt = $"""
            You are a professional nutritionist and health coach reviewing a client's weekly nutrition log.
            Provide an honest, encouraging, and specific weekly review.
            Be concrete and actionable — avoid generic advice.

            --- Weekly nutrition data ---
            {weekSummary}
            --- End of data ---
            """;

        var body = new
        {
            contents = new[] { new { parts = new[] { new { text = prompt } } } },
            generationConfig = new
            {
                responseMimeType = "application/json",
                responseSchema = schema,
                thinkingConfig = new { thinkingBudget = -1 }
            }
        };

        var url = string.Format(StreamEndpoint, _apiKey);
        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(body)
        };

        var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException($"Gemini API error {(int)response.StatusCode}: {error}");
        }

        using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream && !ct.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(ct);
            if (line == null || !line.StartsWith("data: ")) continue;

            JsonElement chunk;
            try { chunk = JsonSerializer.Deserialize<JsonElement>(line["data: ".Length..]); }
            catch { continue; }

            if (!chunk.TryGetProperty("candidates", out var candidates) || candidates.GetArrayLength() == 0) continue;
            if (!candidates[0].TryGetProperty("content", out var content)) continue;
            if (!content.TryGetProperty("parts", out var parts)) continue;

            foreach (var part in parts.EnumerateArray())
            {
                if (!part.TryGetProperty("text", out var textEl)) continue;
                var text = textEl.GetString() ?? "";
                if (string.IsNullOrEmpty(text)) continue;
                bool isThought = part.TryGetProperty("thought", out var thoughtEl) && thoughtEl.GetBoolean();
                yield return (isThought, text);
            }
        }
    }

    public async IAsyncEnumerable<(bool IsThinking, string Text)> StreamChallengeSummaryAsync(
        string challengeText,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var schema = new
        {
            type = "OBJECT",
            properties = new
            {
                winner_analysis   = new { type = "STRING",  description = "Analysis of the winner's performance and strategy" },
                highlights        = new { type = "ARRAY", items = new { type = "STRING" }, description = "3-5 notable highlights from the challenge" },
                improvement_tips  = new { type = "ARRAY", items = new { type = "STRING" }, description = "3 specific improvement tips for participants" },
                fun_stats         = new { type = "ARRAY", items = new { type = "STRING" }, description = "3-5 fun or surprising statistics from the challenge" }
            },
            required = new[] { "winner_analysis", "highlights", "improvement_tips", "fun_stats" }
        };

        var prompt = $"""
            You are a sports nutritionist and fitness coach reviewing a 7-day high protein challenge.
            Provide an entertaining, motivating, and specific summary of the challenge results.
            Reference actual numbers from the data. Include fun comparisons (e.g. "That's like eating X chicken breasts!").
            Be encouraging even for participants who didn't hit their targets every day.

            --- Challenge data ---
            {challengeText}
            --- End of data ---
            """;

        var body = new
        {
            contents = new[] { new { parts = new[] { new { text = prompt } } } },
            generationConfig = new
            {
                responseMimeType = "application/json",
                responseSchema = schema,
                thinkingConfig = new { thinkingBudget = -1 }
            }
        };

        var url = string.Format(StreamEndpoint, _apiKey);
        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(body)
        };

        var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException($"Gemini API error {(int)response.StatusCode}: {error}");
        }

        using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream && !ct.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(ct);
            if (line == null || !line.StartsWith("data: ")) continue;

            JsonElement chunk;
            try { chunk = JsonSerializer.Deserialize<JsonElement>(line["data: ".Length..]); }
            catch { continue; }

            if (!chunk.TryGetProperty("candidates", out var candidates) || candidates.GetArrayLength() == 0) continue;
            if (!candidates[0].TryGetProperty("content", out var content)) continue;
            if (!content.TryGetProperty("parts", out var parts)) continue;

            foreach (var part in parts.EnumerateArray())
            {
                if (!part.TryGetProperty("text", out var textEl)) continue;
                var text = textEl.GetString() ?? "";
                if (string.IsNullOrEmpty(text)) continue;
                bool isThought = part.TryGetProperty("thought", out var thoughtEl) && thoughtEl.GetBoolean();
                yield return (isThought, text);
            }
        }
    }

    private object BuildBody(string? userText, byte[]? fileData, string? mimeType, string? contextSummary, bool withThinking)
    {
        var parts = new List<object>();

        if (fileData != null && mimeType != null)
        {
            parts.Add(new
            {
                inline_data = new
                {
                    mime_type = mimeType,
                    data = Convert.ToBase64String(fileData)
                }
            });
        }

        parts.Add(new { text = BuildPrompt(userText, contextSummary) });

        var schema = new
        {
            type = "OBJECT",
            properties = new
            {
                food_description = new { type = "STRING", description = "Brief description of the food/meal observed" },
                calories_min = new { type = "INTEGER", description = "Lower bound of estimated calorie range" },
                calories_max = new { type = "INTEGER", description = "Upper bound of estimated calorie range" },
                proteins = new { type = "NUMBER", description = "Estimated protein content in grams" },
                carbohydrates = new { type = "NUMBER", description = "Estimated carbohydrate content in grams" },
                fats = new { type = "NUMBER", description = "Estimated fat content in grams" },
                fiber = new { type = "NUMBER", description = "Estimated dietary fiber content in grams" },
                sugar = new { type = "NUMBER", description = "Estimated total sugar content in grams" },
                sodium = new { type = "NUMBER", description = "Estimated sodium content in milligrams" },
                whats_good = new { type = "ARRAY", items = new { type = "STRING" }, description = "2-3 positive aspects of the meal" },
                what_to_improve = new { type = "ARRAY", items = new { type = "STRING" }, description = "2-3 specific actionable improvement suggestions" },
                score = new { type = "INTEGER", description = "Overall nutritional score from 1 to 10" },
                score_summary = new { type = "STRING", description = "One sentence explaining the score" }
            },
            required = new[] { "food_description", "calories_min", "calories_max", "proteins", "carbohydrates", "fats", "fiber", "sugar", "sodium", "whats_good", "what_to_improve", "score", "score_summary" }
        };

        if (withThinking)
        {
            return new
            {
                contents = new[] { new { parts = parts.ToArray() } },
                generationConfig = new
                {
                    responseMimeType = "application/json",
                    responseSchema = schema,
                    thinkingConfig = new { thinkingBudget = -1 }
                }
            };
        }

        return new
        {
            contents = new[] { new { parts = parts.ToArray() } },
            generationConfig = new
            {
                responseMimeType = "application/json",
                responseSchema = schema
            }
        };
    }

    private static string BuildPrompt(string? userText, string? contextSummary)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("You are a professional nutritionist and health coach.");
        sb.AppendLine("Analyze the meal or nutrition plan provided (image and/or text description).");
        sb.AppendLine("Return accurate nutritional estimates. For calories, provide a realistic min/max range.");
        sb.AppendLine("Score the meal from 1 (very unhealthy) to 10 (excellent nutrition).");
        sb.AppendLine("Be specific and actionable in your improvement suggestions.");

        if (!string.IsNullOrWhiteSpace(contextSummary))
        {
            sb.AppendLine();
            sb.AppendLine("--- User's recent nutrition history (last 7 days) ---");
            sb.AppendLine(contextSummary);
            sb.AppendLine("--- End of history ---");
            sb.AppendLine("Use this context to make your feedback more personalised. Reference patterns you notice.");
        }

        if (!string.IsNullOrWhiteSpace(userText))
        {
            sb.AppendLine();
            sb.AppendLine("User's description of the current meal:");
            sb.AppendLine(userText);
        }

        return sb.ToString();
    }
}
