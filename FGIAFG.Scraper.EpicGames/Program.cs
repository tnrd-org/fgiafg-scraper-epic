using FGIAFG.Scraper.EpicGames.Database;
using FGIAFG.Scraper.EpicGames.Jobs;
using FGIAFG.Scraper.EpicGames.Scraping;
using Microsoft.EntityFrameworkCore;
using Quartz;
using Serilog;
using DbContext = FGIAFG.Scraper.EpicGames.Database.DbContext;

namespace FGIAFG.Scraper.EpicGames;

internal class Program
{
    public static async Task Main(string[] args)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

        builder.Host.UseSerilog((context, configuration) =>
        {
            configuration
                .WriteTo.Console()
                .MinimumLevel.Debug();
        });

        builder.Services.AddDbContext<DbContext>(options =>
        {
            string path = Path.GetDirectoryName(builder.Configuration["DataSource"])!;
            if (!string.IsNullOrEmpty(path) && !Path.Exists(path))
                Directory.CreateDirectory(path);

            options.UseSqlite("Data Source=" + builder.Configuration["DataSource"]);
        });
        builder.Services.AddTransient<EpicGamesScraper>();
        builder.Services.AddHttpClient();

        builder.Services.AddQuartz(q =>
        {
            JobKey key = new JobKey("ScrapeAndStoreJob");
            q.AddJob<ScrapeAndStoreJob>(o => o.WithIdentity(key));
            q.AddTrigger(o =>
                o.ForJob(key).WithIdentity("ScrapeAndStoreTrigger")
                    .WithCronSchedule(builder.Configuration["Schedule"] ?? "0 0/15 * ? * * *"));
            q.UseMicrosoftDependencyInjectionJobFactory();
        });

        builder.Services.AddQuartzHostedService();

        WebApplication app = builder.Build();

        using (IServiceScope scope = app.Services.CreateScope())
        {
            DbContext dbContext = scope.ServiceProvider.GetRequiredService<DbContext>();
            ILogger<Program> logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
#if DEBUG

            logger.LogInformation("Deleting database...");
            await dbContext.Database.EnsureDeletedAsync();
#endif
            logger.LogInformation("Creating database...");
            await dbContext.Database.EnsureCreatedAsync();
        }

        app.MapGet("/", GetGames);

        await app.RunAsync();
    }

    private static Task<IResult> GetGames(DbContext dbContext, HttpContext context)
    {
        IQueryable<GameModel> gameModels =
            dbContext.Games.Where(x => x.StartDate <= DateTime.Now && x.EndDate >= DateTime.Now);

        return Task.FromResult<IResult>(TypedResults.Ok(gameModels));
    }
}
