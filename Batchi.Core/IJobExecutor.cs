namespace Batchi.Core;

/// <summary>
/// Defines a unit of work to be executed by the Batchi job processor.
/// The inbound message provides the data for the job to process and is
/// encapsulated by <see cref="JobContext{TMessage}"/> providing relevant job metadata.
/// </summary>
/// <typeparam name="TMessage"></typeparam>
public interface IJobExecutor<TMessage>
    where TMessage : class
{
    public Task RunAsync(Batch<TMessage> batch, CancellationToken ct);
}