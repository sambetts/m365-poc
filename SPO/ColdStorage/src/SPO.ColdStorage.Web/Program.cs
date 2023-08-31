using Microsoft.EntityFrameworkCore;
using SPO.ColdStorage.Entities;
using SPO.ColdStorage.Entities.Configuration;
using Microsoft.Identity.Web;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using SPO.ColdStorage.Migration.Engine;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var config = new Config(builder.Configuration);

builder.Services.AddSingleton(config);
builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<SPOColdStorageDbContext>(
    options => options.UseSqlServer(config.ConnectionStrings.SQLConnectionString));
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));
builder.Services.AddApplicationInsightsTelemetry(builder.Configuration["APPINSIGHTS_CONNECTIONSTRING"]);

var telemetry = new DebugTracer(config.AppInsightsInstrumentationKey, "Web Start");
builder.Services.AddSingleton(telemetry);

var app = builder.Build();

try
{
    // Init DB if needed
    var db = new SPOColdStorageDbContext(config);
    await DbInitializer.Init(db, config.DevConfig);

    // Configure the HTTP request pipeline.
    if (!app.Environment.IsDevelopment())
    {
        // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
        app.UseHsts();
    }

    app.UseHttpsRedirection();
    app.UseStaticFiles();
    app.UseRouting();
    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllerRoute(
        name: "default",
        pattern: "{controller}/{action=Index}/{id?}");

    app.MapFallbackToFile("index.html"); ;

    app.Run();
}
catch (Exception ex)
{
    telemetry.TrackTrace($"Failed to start web - {ex.Message}", Microsoft.ApplicationInsights.DataContracts.SeverityLevel.Critical);
    telemetry.TrackException(ex);
}



