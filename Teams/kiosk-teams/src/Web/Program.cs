using Microsoft.EntityFrameworkCore;
using Entities.Configuration;
using Entities;
using Engine;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var config = new AppConfig(builder.Configuration);

builder.Services.AddSingleton(config);
builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<ILocationIpRuleLoader, SqlAndHttpLocationIpRuleLoader>();
builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<AppDbContext>(
    options => options.UseSqlServer(config.ConnectionStrings.SQLConnectionString));
builder.Services.AddApplicationInsightsTelemetry(builder.Configuration["APPINSIGHTS_CONNECTIONSTRING"]);

var app = builder.Build();

// Init DB if needed
var db = new AppDbContext(config);
await DbInitializer.Init(db);

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

