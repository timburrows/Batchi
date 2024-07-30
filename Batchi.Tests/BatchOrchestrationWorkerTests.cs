using System.Threading.Channels;
using AutoFixture;
using Batchi.Core;
using Batchi.Core.Models;
using Batchi.Core.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moq;

namespace Batchi.Tests;

public class InsertBookRequest
{
    public TestBooks TestBook { get; set; }
}

public class BatchOrchestrationWorkerTests
{
    private readonly Channel<JobContext<InsertBookRequest>> _jobContextCh;
    private readonly Channel<JobDetails> _jobDetailsCh;
    private readonly Mock<IDbContextFactory<PlatformContext>> _mockDbFactory;
    private readonly Mock<IJobExecutor<InsertBookRequest>> _mockExecutor;

    public BatchOrchestrationWorkerTests()
    {
        _jobContextCh = Channel.CreateUnbounded<JobContext<InsertBookRequest>>(
            new UnboundedChannelOptions { SingleReader = true, SingleWriter = true, });
        _jobDetailsCh = Channel.CreateUnbounded<JobDetails>(
            new UnboundedChannelOptions { SingleReader = false, SingleWriter = true, });
        
        var dbOptions = new DbContextOptionsBuilder<PlatformContext>()
            .UseInMemoryDatabase("FakeBatchi")
            .Options;

        _mockDbFactory = new Mock<IDbContextFactory<PlatformContext>>();
        _mockDbFactory.Setup(_ => _.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new PlatformContext(dbOptions));
        
        _mockExecutor = new Mock<IJobExecutor<InsertBookRequest>>();
        _mockExecutor.Setup(_ => _.RunAsync(It.IsAny<Batch<InsertBookRequest>>(), It.IsAny<CancellationToken>()));
    }
    
    [Fact]
    public void DoAsync_WithMaxJobsPerBatch_RunsNumTimes()
    {
        const int numJobsQueued = 10;
        const int expectedJobsPerBatch = 2;
        const int expecetedBatches = 5;

        var options = CreateOptions(expectedJobsPerBatch, int.MaxValue);
        
        CreateJobs(numJobsQueued);

        var sut = new BatchOrchestrationWorker<InsertBookRequest>(
            _jobContextCh.Reader,
            _jobDetailsCh.Writer,
            options,
            _mockDbFactory.Object,
            _mockExecutor.Object
        );

        _ = sut.DoAsync(It.IsAny<CancellationToken>());
        
        _mockExecutor.Verify(executor => executor.RunAsync(
            It.IsAny<Batch<InsertBookRequest>>(), 
            It.IsAny<CancellationToken>()), 
            Times.Exactly(expecetedBatches)
        );
    }

    private void CreateJobs(int numJobsQueued)
    {
        var fixture = new Fixture();
        var jobs = fixture.Build<JobContext<InsertBookRequest>>().CreateMany(numJobsQueued);
        foreach (var jobContext in jobs)
        {
            _jobContextCh.Writer.TryWrite(jobContext);
        }
    }

    [Fact]
    public void DoAsync_WithFetchDuration_StopsAfterDuration()
    {
        const int numJobsQueued = 10;
        const int expectedJobsDuration = 200;

        var options = CreateOptions(numJobsQueued, expectedJobsDuration);
        
        CreateJobs(numJobsQueued);


        var sut = new BatchOrchestrationWorker<InsertBookRequest>(
            _jobContextCh.Reader,
            _jobDetailsCh.Writer,
            options,
            _mockDbFactory.Object,
            _mockExecutor.Object
        );

        _ = sut.DoAsync(It.IsAny<CancellationToken>());
        
        _mockExecutor.Verify(executor => executor.RunAsync(
                It.IsAny<Batch<InsertBookRequest>>(), 
                It.IsAny<CancellationToken>()), 
            Times.AtMost(5)
        );
    }
    
    [Fact]
    public void DoAsync_ShutdownInvoked_StopsBeforeNextBatch()
    {
        const int numJobsQueued = 50;
        const int maxJobs = 10;
        const int jobDuration = 50000;

        var options = CreateOptions(maxJobs, jobDuration);
        
        CreateJobs(numJobsQueued);

        var sut = new BatchOrchestrationWorker<InsertBookRequest>(
            _jobContextCh.Reader,
            _jobDetailsCh.Writer,
            options,
            _mockDbFactory.Object,
            _mockExecutor.Object
        );

        Task.Run(async () =>
        {
            await Task.Delay(TimeSpan.FromSeconds(2));
            sut.Shutdown();
        });
        
        _ = sut.DoAsync(It.IsAny<CancellationToken>());
        
        _mockExecutor.Verify(executor => executor.RunAsync(
                It.IsAny<Batch<InsertBookRequest>>(), 
                It.IsAny<CancellationToken>()), 
            Times.AtMost(5)
        );
    }

    private static IOptions<BatchiOptions> CreateOptions(int jobsPerBatch, int jobsDuration)
    {
        var options = Options.Create(new BatchiOptions()
        {
            Jobs = new List<BatchiJobOptions>()
            {
                new()
                {
                    FetchJobsDuration = jobsDuration,
                    MaxJobsPerBatch = jobsPerBatch,
                }
            }
        });
        return options;
    }
}