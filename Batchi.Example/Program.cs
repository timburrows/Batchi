using Batchi.Example;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);
var startup = new Startup(builder);
startup.ConfigureServices(builder.Services);
var app = builder.Build();
app.Run();
