using Azure.Identity;
using CommonUtils;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Identity.Web;
using Microsoft.Graph;
using SPOAzBlob.Engine;

var builder = Microsoft.AspNetCore.Builder.WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllersWithViews();

var config = new Config(builder.Configuration);
builder.Services.AddSingleton(config);
builder.Services.AddApplicationInsightsTelemetry(config.AppInsightsInstrumentationKey);
var telemetry = new DebugTracer(config.AppInsightsInstrumentationKey, "Web");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));
builder.Services.AddSingleton(telemetry);

// Graph client
var options = new TokenCredentialOptions
{
    AuthorityHost = AzureAuthorityHosts.AzurePublicCloud
};
var scopes = new[] { "https://graph.microsoft.com/.default" };
var clientSecretCredential = new ClientSecretCredential(config.AzureAdConfig.TenantId, config.AzureAdConfig.ClientID, config.AzureAdConfig.Secret, options);
var client = new GraphServiceClient(clientSecretCredential, scopes);
builder.Services.AddSingleton(client);

var app = builder.Build();

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
