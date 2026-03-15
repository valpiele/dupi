using System.Text.Json.Serialization;

namespace dupi.Models;

public class NutritionAnalysis
{
    [JsonPropertyName("food_description")]
    public string FoodDescription { get; set; } = string.Empty;

    [JsonPropertyName("calories_min")]
    public int CaloriesMin { get; set; }

    [JsonPropertyName("calories_max")]
    public int CaloriesMax { get; set; }

    [JsonPropertyName("proteins")]
    public double Proteins { get; set; }

    [JsonPropertyName("carbohydrates")]
    public double Carbohydrates { get; set; }

    [JsonPropertyName("fats")]
    public double Fats { get; set; }

    [JsonPropertyName("whats_good")]
    public List<string> WhatsGood { get; set; } = new();

    [JsonPropertyName("what_to_improve")]
    public List<string> WhatToImprove { get; set; } = new();

    [JsonPropertyName("fiber")]
    public double Fiber { get; set; }

    [JsonPropertyName("sugar")]
    public double Sugar { get; set; }

    [JsonPropertyName("sodium")]
    public double Sodium { get; set; }

    [JsonPropertyName("score")]
    public int Score { get; set; }

    [JsonPropertyName("score_summary")]
    public string ScoreSummary { get; set; } = string.Empty;
}
