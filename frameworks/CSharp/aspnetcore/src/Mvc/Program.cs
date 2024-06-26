using System.Text.Encodings.Web;
using System.Text.Unicode;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Mvc;
using Mvc.Database;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Instrumentation;
using OpenTelemetry.Metrics;


var builder = WebApplication.CreateBuilder(args);


// Remove logging as this is not required for the benchmark

#if LOGGING_ENABLED
builder.Logging.ClearProviders(); // Clear default logging providers
builder.Logging.AddOpenTelemetry(options =>
{
    options.AddOtlpExporter(otlpOptions =>
    {
        otlpOptions.Endpoint = new Uri("http://172.30.0.53:4317");
    });
});

builder.Services.AddHttpLogging(o => { });

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("serviceName"))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddOtlpExporter(otlpOptions =>
        {
            otlpOptions.Endpoint = new Uri("http://172.30.0.53:4317");
        }))
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddOtlpExporter(otlpOptions =>
        {
            otlpOptions.Endpoint = new Uri("http://172.30.0.53:4317");
        }));

Console.WriteLine("Logging is enabled!");
#else
builder.Logging.ClearProviders();
#endif

// Load custom configuration
var appSettings = new AppSettings();
builder.Configuration.Bind(appSettings);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddTransient<Db>();
builder.Services.AddSingleton(appSettings);
builder.Services.AddDbContextPool<ApplicationDbContext>(
    options => options
        .UseNpgsql(appSettings.ConnectionString, o => o.ExecutionStrategy(d => new NonRetryingExecutionStrategy(d)))
        .EnableThreadSafetyChecks(false)
        );
builder.Services.AddSingleton(serviceProvider =>
{
    var settings = new TextEncoderSettings(UnicodeRanges.BasicLatin, UnicodeRanges.Katakana, UnicodeRanges.Hiragana);
    settings.AllowCharacter('\u2014'); // allow EM DASH through
    return HtmlEncoder.Create(settings);
});

var app = builder.Build();

#if LOGGING_ENABLED
app.UseHttpLogging();
#endif

app.UseRouting();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Lifetime.ApplicationStarted.Register(() => Console.WriteLine("Application started. Press Ctrl+C to shut down."));
app.Lifetime.ApplicationStopping.Register(() => Console.WriteLine("Application is shutting down..."));

app.Run();
