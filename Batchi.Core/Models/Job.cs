namespace Batchi.Core.Models;

public class Job
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public int JobDetailsId { get; set; }
    public JobDetails JobDetails { get; set; } = new();
}