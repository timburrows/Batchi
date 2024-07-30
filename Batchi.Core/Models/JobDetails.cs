namespace Batchi.Core.Models;

public enum JobStatus
{
    Pending,
    Working,
    Completed,
    
    Failed
}

public class JobDetails
{
    public int Id { get; set; }
    public DateTime? LastModifiedOn { get; set; }
    public JobStatus JobStatus { get; set; }
    
    // todo: persist the original job message for retry/logging purposes

    public JobDetails()
    {
        LastModifiedOn = DateTime.UtcNow;
        JobStatus = JobStatus.Pending;
    }
}