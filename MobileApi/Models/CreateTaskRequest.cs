using System.Text.Json.Serialization;

namespace MobileApi.Models;

public class CreateTaskRequest
{
    [JsonPropertyName("solutionInfo")]
    public SolutionList SolutionList { get; set; } = new SolutionList();

    public List<TaskItem> Tasks { get; set; } = new List<TaskItem>();

    [JsonPropertyName("userList")]
    public List<User> Users { get; set; } = new List<User>();
}