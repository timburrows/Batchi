using System.Threading.Channels;
using Batchi.Core.Models;
using Batchi.Core.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Batchi.Core;

public static class Extensions
{
    /// <summary>
    /// Registers common Batchi dependencies and configuration
    /// </summary>
    /// <param name="services"></param>
    /// <param name="configuration"></param>
    /// <returns></returns>
    /// <exception cref="ApplicationException"></exception>
    public static IServiceCollection AddBatchi(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = 
            configuration.GetConnectionString("BatchiDatabase") 
            ?? throw new ApplicationException("No database connection string");

        services.AddDbContextFactory<PlatformContext>(options =>
        {
            options.UseNpgsql(connectionString, (o) =>
            {
                o.MigrationsAssembly("Batchi.Migrations");
                o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                o.CommandTimeout(180);
            });
        }, ServiceLifetime.Transient);
        
        services.AddOptions<BatchiOptions>().Bind(configuration.GetSection(BatchiOptions.Batchi));

        return services;
    }

    /// <summary>
    /// Registers an instance of <see cref="IJobExecutor{TMessage}"/>, subscribed to incoming messages of type TMessage
    /// </summary>
    /// <param name="services"></param>
    /// <typeparam name="TMessage"></typeparam>
    /// <typeparam name="TProcessor"></typeparam>
    /// <returns></returns>
    public static IServiceCollection AddProcessor<TMessage, TProcessor>(this IServiceCollection services)
        where TMessage : class
        where TProcessor : class, IJobExecutor<TMessage>
    {
        var channel = Channel.CreateUnbounded<JobContext<TMessage>>(
            new UnboundedChannelOptions { SingleReader = true, SingleWriter = true, });
        
        var channel2 = Channel.CreateUnbounded<JobDetails>(
            new UnboundedChannelOptions { SingleReader = false, SingleWriter = true, });

        services.AddHostedService<BatchOrchestrationWorker<TMessage>>();

        services
            .AddSingleton(channel.Reader)
            .AddSingleton(channel.Writer)
            .AddSingleton(channel2.Reader)
            .AddSingleton(channel2.Writer);
            

        services.AddTransient<IJobExecutor<TMessage>, TProcessor>();
        
        return services;
    }
}