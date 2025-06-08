namespace MobileApi.Models;

public class SolutionList
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? FullDescription { get; set; }
    public List<bool>? Solutions { get; set; }
}