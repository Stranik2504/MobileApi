using System.Text.Json.Serialization;

namespace MobileApi.Utils;

public class FullUser
{
    public int Id { get; set; }

    [JsonPropertyName("is_admin")]
    public bool IsAdmin { get; set; }

    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    [JsonPropertyName("pass_count")]
    public int PassCount { get; set; }

    public int Count { get; set; }

    [JsonPropertyName("error_count")]
    public int ErrorCount { get; set; }

    [JsonPropertyName("count_tasks")]
    public int CountTasks { get; set; }

    public string Token { get; set; } = string.Empty;
}