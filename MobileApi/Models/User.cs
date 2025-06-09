using System.Text.Json.Serialization;

namespace MobileApi.Models;

public class User
{
    public int Id { get; set; }
    [JsonPropertyName("name")]
    public string Username { get; set; }
}