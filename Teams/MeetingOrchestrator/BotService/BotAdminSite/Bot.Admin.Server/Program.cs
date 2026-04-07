using Bot.Admin.Services;
using Microsoft.Identity.Web;
using Microsoft.Extensions.Azure;

var builder = WebApplication.CreateBuilder(args);

// --- Authentication (MSAL / Entra ID) ---
builder.Services.AddMicrosoftIdentityWebApiAuthentication(builder.Configuration);

// --- Application services ---
builder.Services.AddSingleton<IScriptStorageService, ScriptStorageService>();
builder.Services.AddHttpClient<IBotProxyService, BotProxyService>();

builder.Services.AddControllers();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        // Allow the Vite dev server during development
        policy.WithOrigins("https://localhost:5173", "http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});
builder.Services.AddAzureClients(clientBuilder =>
{
    clientBuilder.AddBlobServiceClient(builder.Configuration["StorageConnection:blobServiceUri"]!).WithName("StorageConnection");
    clientBuilder.AddQueueServiceClient(builder.Configuration["StorageConnection:queueServiceUri"]!).WithName("StorageConnection");
    clientBuilder.AddTableServiceClient(builder.Configuration["StorageConnection:tableServiceUri"]!).WithName("StorageConnection");
});

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// In production, serve the built SPA from wwwroot
app.MapFallbackToFile("index.html");

// Seed a default sample script if storage is empty
var scriptService = app.Services.GetRequiredService<IScriptStorageService>();
await scriptService.EnsureDefaultScriptAsync();

app.Run();
