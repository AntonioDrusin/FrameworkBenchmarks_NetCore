using System.Text.Encodings.Web;
using System.Text.Unicode;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Mvc;
using Mvc.Database;

var builder = WebApplication.CreateBuilder(args);

// Remove logging as this is not required for the benchmark
#if LOGGING_ENABLED
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
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

// Enable logging of each request
#if LOGGING_ENABLED
app.Use(async (context, next) =>
{
    var logger = app.Logger;
    logger.LogInformation("Handling request: {Method} {Path}", context.Request.Method, context.Request.Path);
    await next.Invoke();
    logger.LogInformation("Finished handling request.");
});
#endif

app.UseRouting();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Lifetime.ApplicationStarted.Register(() => Console.WriteLine("Application started. Press Ctrl+C to shut down."));
app.Lifetime.ApplicationStopping.Register(() => Console.WriteLine("Application is shutting down..."));

app.Run();
