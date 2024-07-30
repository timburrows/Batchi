using Batchi.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Batchi.Example;

internal class Startup
{
    private IConfiguration Configuration { get; }

    public Startup(HostApplicationBuilder builder)
    {
        builder.Configuration
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile($"appsettings.json", optional: false, reloadOnChange: true);

        Configuration = builder.Configuration;
    }

    public void ConfigureServices(IServiceCollection services)
    {
        // This is important! We register all the dependencies for Batchi here
        services.AddBatchi(Configuration);

        // Don't forget to register your Job!
        services.AddProcessor<InsertBookRequest, MyProcessor>();

        // I'm just a dummy client pretending (poorly!) to fetch messages from a pub/sub,
        // this is where we feed Batchi with Job request messages
        services.AddHostedService<MySubscriberClient>();
        
    }
}