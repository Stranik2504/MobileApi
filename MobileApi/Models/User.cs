using System.Text.Json.Serialization;

namespace MobileApi.Models;

public class User
{
    [JsonPropertyName("id")]
    public int Id { get; set; }
    [JsonPropertyName("name")]
    public string Username { get; set; }
    [JsonPropertyName("selected")]
    public bool Selected { get; set; }
}