using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Bot.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Web;
using SPO.ColdStorage.Entities;
using SPO.ColdStorage.Entities.Configuration;
using SPO.ColdStorage.Migration.Engine;
using System.Threading.Tasks;

namespace Web.Server;

public class Program
{
    public async static Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Services.AddControllers().AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        });

        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        var config = new Config(builder.Configuration);
        builder.Services.AddSingleton(config);

        // Register DebugTracer for dependency injection
        var debugTracer = new DebugTracer(config.AppInsightsInstrumentationKey, "Web.Server");
        builder.Services.AddSingleton(debugTracer);


        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));

        // UsageStatsReport
        builder.Services.AddDbContext<SPOColdStorageDbContext>(options =>
            options.UseSqlServer(config.ConnectionStrings.SQLConnectionString));


        var app = builder.Build();


        // Ensure DB
        var optionsBuilder = new DbContextOptionsBuilder<SPOColdStorageDbContext>();
        optionsBuilder.UseSqlServer(config.ConnectionStrings.SQLConnectionString);

        using (var db = new SPOColdStorageDbContext(config))
        {
            var logger = LoggerFactory.Create(c =>
            {
                c.AddConsole();
            }).CreateLogger("DB init");
            logger.LogInformation($"Using SQL connection-string: {config.ConnectionStrings.SQLConnectionString}");

            await DbInitializer.Init(db, config.DevConfig);
        }

        // https://learn.microsoft.com/en-us/visualstudio/javascript/tutorial-asp-net-core-with-react?view=vs-2022#publish-the-project
        app.UseDefaultFiles();
        app.UseStaticFiles();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseAuthorization();

        app.MapControllers();

        app.Run();
    }
}
