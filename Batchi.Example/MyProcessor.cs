using Batchi.Core;
using Batchi.Core.Models;
using Batchi.Core.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Batchi.Example;

internal class InsertBookRequest
{
    public TestBooks TestBook { get; set; }
}

internal class MyProcessor(IDbContextFactory<PlatformContext> dbContextFactory) : IJobExecutor<InsertBookRequest>
{
    public async Task RunAsync(Batch<InsertBookRequest> batch, CancellationToken ct)
    {
        var db = await dbContextFactory.CreateDbContextAsync(ct);

        var booksToAdd = batch.Select(jobContext => jobContext.Message.TestBook).ToList();
        await db.TestBooks.AddRangeAsync(booksToAdd, ct);

        var jobs = await db.Jobs.Include(job => job.JobDetails)
            .Where(_ => batch.Contexts.Select(_ => _.JobId).Contains(_.Id)).ToListAsync(ct);
        
        foreach (var context in batch.Contexts)
        {
            var job = jobs.FirstOrDefault(_ => _.Id == context.JobId);
            if (job != null)
            {
                job.JobDetails.JobStatus = JobStatus.Completed;
            }
        }

        await db.SaveChangesAsync(ct);
    }
}