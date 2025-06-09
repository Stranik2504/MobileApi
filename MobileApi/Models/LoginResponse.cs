using System.Text.Json.Serialization;

namespace MobileApi.Models;

public class LoginResponse
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("isAdmin")]
    public bool IsAdmin { get; set; }

    [JsonPropertyName("token")]
    public string Token { get; set; } = string.Empty;

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
}