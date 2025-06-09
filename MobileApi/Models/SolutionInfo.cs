namespace MobileApi.Models;

public class SolutionInfo
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int CountAttempts { get; set; }
    public int CountUserAttempts { get; set; }
}