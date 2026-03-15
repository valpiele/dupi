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
        string? userText, byte[]? fileData, string? mimeType,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var url = string.Format(StreamEndpoint, _apiKey);
        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(BuildBody(userText, fileData, mimeType, withThinking: true))
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

    private object BuildBody(string? userText, byte[]? fileData, string? mimeType, bool withThinking)
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

        parts.Add(new { text = BuildPrompt(userText) });

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
                whats_good = new { type = "ARRAY", items = new { type = "STRING" }, description = "2-3 positive aspects of the meal" },
                what_to_improve = new { type = "ARRAY", items = new { type = "STRING" }, description = "2-3 specific actionable improvement suggestions" },
                score = new { type = "INTEGER", description = "Overall nutritional score from 1 to 10" },
                score_summary = new { type = "STRING", description = "One sentence explaining the score" }
            },
            required = new[] { "food_description", "calories_min", "calories_max", "proteins", "carbohydrates", "fats", "whats_good", "what_to_improve", "score", "score_summary" }
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

    private static string BuildPrompt(string? userText)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("You are a professional nutritionist and health coach.");
        sb.AppendLine("Analyze the meal or nutrition plan provided (image and/or text description).");
        sb.AppendLine("Return accurate nutritional estimates. For calories, provide a realistic min/max range.");
        sb.AppendLine("Score the meal from 1 (very unhealthy) to 10 (excellent nutrition).");
        sb.AppendLine("Be specific and actionable in your improvement suggestions.");

        if (!string.IsNullOrWhiteSpace(userText))
        {
            sb.AppendLine();
            sb.AppendLine("User's description:");
            sb.AppendLine(userText);
        }

        return sb.ToString();
    }
}
