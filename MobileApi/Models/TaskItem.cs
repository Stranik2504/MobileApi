using System.Text.Json.Serialization;

namespace MobileApi.Models;

public class TaskItem
{
    public string Question { get; set; } = string.Empty;
    [JsonPropertyName("options")]
    public List<AnswerOption> AnswerOptions { get; set; } = new List<AnswerOption>();
}