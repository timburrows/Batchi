using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Batchi.Core.Models;
using Batchi.Core.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Batchi.Core;

public sealed class BatchOrchestrationWorker<TMessage>(
    ChannelReader<JobContext<TMessage>> chReader,
    ChannelWriter<JobDetails> chWriter,
    IOptions<BatchiOptions> batchOptions,
    IDbContextFactory<PlatformContext> dbContextFactory,
    IJobExecutor<TMessage> executor
) : BackgroundService
    where TMessage : class
{
    private readonly Batch<TMessage> _batch = new();
    private bool _shouldShutdown = false;

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        await DoAsync(ct);
    }
    
    public void Shutdown()
    {
        _shouldShutdown = true;
    }

    internal async Task DoAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested && !_shouldShutdown)
        {
            await DispatchJobsAsync(ct);
        }
    }

    private async Task DispatchJobsAsync(CancellationToken ct)
    {
        try
        {
            // todo: fixme. need to support multiple Job configurations
            var jobOptions = batchOptions.Value.Jobs.First();

            var waitToReadCts = new CancellationTokenSource();
            waitToReadCts.CancelAfter(TimeSpan.FromMilliseconds(jobOptions.FetchJobsDuration.GetValueOrDefault()));

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(waitToReadCts.Token, ct);
            var maxJobsPerBatch = jobOptions.MaxJobsPerBatch.GetValueOrDefault();
            await foreach (var batch in chReader.ReadBatch(maxJobsPerBatch).WithCancellation(cts.Token))
            {
                foreach (var message in batch)
                {
                    var job = await CreateJob(message.JobName, ct);
                    message.JobId = job.Id;
                    _batch.Contexts.Add(message);
                }

                await RunBatchAsync(ct);

                if (_shouldShutdown)
                {
                    return;
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    private async Task<Job> CreateJob(string jobName, CancellationToken ct)
    {
        var db = await dbContextFactory.CreateDbContextAsync(ct);

        var jobDetails = new JobDetails();
        var job = await db.Jobs.AddAsync(new Job
        {
            Name = jobName,
            JobDetailsId = jobDetails.Id
        }, ct);

        await db.SaveChangesAsync(ct);
        return job.Entity;
    }

    private async Task RunBatchAsync(CancellationToken ct)
    {
        var db = await dbContextFactory.CreateDbContextAsync(ct);
        await executor.RunAsync(_batch, ct);

        var jobDetails = await db.JobDetails.Where(_ => _batch.Contexts.Select(_ => _.JobId).Contains(_.Id))
            .ToListAsync(ct);
        
        foreach (var jobContext in _batch)
        {
            var job = jobDetails.FirstOrDefault(_ => _.Id == jobContext.JobId);
            if (job != null)
            {
                chWriter.TryWrite(job);
            }
        }

        _batch.Contexts.Clear();
    }
}

internal static class ChannelExtensions
{
    public static IAsyncEnumerable<T[]> ReadBatch<T>(this ChannelReader<T> source, int maxItems = -1)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (maxItems == -1) maxItems = Array.MaxLength;
        ArgumentOutOfRangeException.ThrowIfLessThan(maxItems, 1);
        return Read();

        async IAsyncEnumerable<T[]> Read([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            while (await source.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
            {
                List<T> buffer = [];
                while (buffer.Count < maxItems && source.TryRead(out var item))
                {
                    buffer.Add(item);
                }

                if (buffer.Count > 0)
                {
                    yield return buffer.ToArray();
                }
            }
        }
    }
}