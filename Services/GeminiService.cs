using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace dupi.Services;

public class GeminiService
{
    private readonly HttpClient _http;
    private readonly string _apiKey;
    private const string Endpoint = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent?key={0}";

    public GeminiService(HttpClient http, IConfiguration config)
    {
        _http = http;
        _apiKey = config["Gemini:ApiKey"]
            ?? throw new InvalidOperationException("Gemini:ApiKey is not configured.");
    }

    public async Task<string> AnalyzeNutritionAsync(string? userText, byte[]? fileData, string? mimeType)
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

        var body = new
        {
            contents = new[] { new { parts = parts.ToArray() } }
        };

        var url = string.Format(Endpoint, _apiKey);
        var response = await _http.PostAsJsonAsync(url, body);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Gemini API error {(int)response.StatusCode}: {error}");
        }

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        return json
            .GetProperty("candidates")[0]
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString() ?? "No analysis available.";
    }

    private static string BuildPrompt(string? userText)
    {
        var sb = new StringBuilder();
        sb.AppendLine("You are a professional nutritionist and health coach.");
        sb.AppendLine("Analyze the meal or nutrition plan provided (image and/or text description) and give a detailed, friendly response.");
        sb.AppendLine("If analyzing an image of food, start by briefly describing what you see.");
        sb.AppendLine();
        sb.AppendLine("Structure your response exactly like this:");
        sb.AppendLine();
        sb.AppendLine("## 🔥 Estimated Calories");
        sb.AppendLine("(total estimate — give a range if unsure)");
        sb.AppendLine();
        sb.AppendLine("## 📊 Macronutrients");
        sb.AppendLine("- **Proteins:** X g");
        sb.AppendLine("- **Carbohydrates:** X g");
        sb.AppendLine("- **Fats:** X g");
        sb.AppendLine();
        sb.AppendLine("## ✅ What's Good");
        sb.AppendLine("(2-3 positive aspects)");
        sb.AppendLine();
        sb.AppendLine("## 💡 What to Improve");
        sb.AppendLine("(2-3 specific, actionable suggestions)");
        sb.AppendLine();
        sb.AppendLine("## ⭐ Overall Score: X/10");
        sb.AppendLine("(one sentence explanation)");

        if (!string.IsNullOrWhiteSpace(userText))
        {
            sb.AppendLine();
            sb.AppendLine("---");
            sb.AppendLine("User's description:");
            sb.AppendLine(userText);
        }

        return sb.ToString();
    }
}
