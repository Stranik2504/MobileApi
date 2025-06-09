using System.Text.Json.Serialization;

namespace MobileApi.Models;

public class UserSolution
{
    [JsonPropertyName("userId")]
    public int Id { get; set; }

    public string Username { get; set; } = string.Empty;

    public bool IsAdmin { get; set; }

    public List<bool> Solutions { get; set; } = [];
}