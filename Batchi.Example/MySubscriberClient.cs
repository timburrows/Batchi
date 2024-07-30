using System.Threading.Channels;
using AutoFixture;
using Batchi.Core;
using Batchi.Core.Models;
using Microsoft.Extensions.Hosting;

namespace Batchi.Example;

internal class MySubscriberClient(
    ChannelWriter<JobContext<InsertBookRequest>> chWriter,
    ChannelReader<JobDetails> chReader) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        // Implement a message broker like GCP Pub/Sub, Kafka, RabbitMQ etc
        for (var i = 0; i < 100; i++)
        {
            var job = FetchMyJob();
            // Pass the messages along the channel to begin batching
            chWriter.TryWrite(job);
        }

        while (await chReader.WaitToReadAsync(ct))
        {
            for (var i = 0; i < 50; i++)
            {
                var item = await chReader.ReadAsync(ct);
                Console.WriteLine($"Job {item.Id} status: {item.JobStatus}");
            }
        }
    }

    private JobContext<InsertBookRequest> FetchMyJob()
    {
        var fixture = new Fixture();
        return new JobContext<InsertBookRequest>
        {
            Message = new InsertBookRequest()
            {
                TestBook = new TestBooks()
                {
                    Title = fixture.Create<string>(),
                    Author = fixture.Create<string>(),
                    ISBN = fixture.Create<string>()
                }
            },
            JobName = nameof(MyProcessor),
        };
    }

    public override Task StartAsync(CancellationToken ct)
    {
        Task.Factory.StartNew(_ => ExecuteAsync(ct), TaskCreationOptions.LongRunning, ct);
        return Task.CompletedTask;
    }
}