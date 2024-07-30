namespace Batchi.Core;

public class BatchiJobOptions
{
    /// <summary>
    /// The name of the configuration section as it appears in appsettings
    /// </summary>
    public const string Jobs = "Jobs";
    
    /// <summary>
    /// The length of time in milliseconds that the Worker will collect Jobs per batch
    /// before the batch is yielded to <see cref="IJobExecutor{TMessage}"/> for execution 
    /// </summary>
    public int? FetchJobsDuration { get; set; }
    
    /// <summary>
    /// The maximum number of Jobs that will be collected per batch
    /// before the batch is yielded to <see cref="IJobExecutor{TMessage}"/> for execution
    /// </summary>
    public int? MaxJobsPerBatch { get; set; }
}

public class BatchiOptions
{
    /// <summary>
    /// The name of the configuration section as it appears in appsettings
    /// </summary>
    public const string Batchi = "Batchi";

    /// <summary>
    /// A collection of <see cref="BatchiJobOptions"/> configuration sections.
    /// Each implementation of <see cref="IJobExecutor{TMessage}"/> should have an equivalent appsettings section 
    /// </summary>
    public IEnumerable<BatchiJobOptions> Jobs { get; set; }

}
