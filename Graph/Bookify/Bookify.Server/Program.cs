using Azure.Identity;
using Bookify.Server; // For AppConfig
using Bookify.Server.Data;
using Bookify.Server.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Graph;

var builder = WebApplication.CreateBuilder(args);

// Create AppConfig from IConfiguration explicitly so it self-populates
var appConfig = new AppConfig(builder.Configuration);

// Validate required settings
if (string.IsNullOrWhiteSpace(appConfig.ConnectionStrings?.DefaultConnection))
    throw new InvalidOperationException("ConnectionStrings:DefaultConnection is not configured");
if (string.IsNullOrWhiteSpace(appConfig.AzureAd?.TenantId) ||
    string.IsNullOrWhiteSpace(appConfig.AzureAd.ClientId) ||
    string.IsNullOrWhiteSpace(appConfig.AzureAd.ClientSecret))
    throw new InvalidOperationException("AzureAd configuration (TenantId / ClientId / ClientSecret) is incomplete");

builder.Services.AddSingleton(appConfig);

// Add services to the container.
builder.Services.AddControllers();


// Configure Entity Framework Core with SQL Server
builder.Services.AddDbContext<BookifyDbContext>(options =>
    options.UseSqlServer(
        appConfig.ConnectionStrings.DefaultConnection,
        sqlOptions => sqlOptions.EnableRetryOnFailure()
    )
);

// Register GraphServiceClient (application permissions)
builder.Services.AddSingleton<GraphServiceClient>(sp =>
{
    var cfg = sp.GetRequiredService<AppConfig>();
    var credential = new ClientSecretCredential(cfg.AzureAd.TenantId, cfg.AzureAd.ClientId, cfg.AzureAd.ClientSecret);
    return new GraphServiceClient(credential, ["https://graph.microsoft.com/.default"]);
});

// Register application services
builder.Services.AddScoped<IBookingCalendarSyncService, BookingCalendarSyncService>();
builder.Services.AddScoped<IBookingService, BookingService>();
builder.Services.AddScoped<IRoomService, RoomService>();
builder.Services.AddSingleton<IExternalCalendarService, GraphCalendarService>();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add CORS policy for development
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        builder => builder
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader());
});

var app = builder.Build();

// Create database if it does not exist (no migrations; schema managed externally)
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<BookifyDbContext>();
        bool isNewDatabase = context.Database.EnsureCreated();
        Console.WriteLine(isNewDatabase
            ? "Database created (EnsureCreated) successfully."
            : "Database already exists (EnsureCreated). Skipping creation.");

        if (isNewDatabase)
        {
            // Assign single configured mailbox UPN to all rooms only for a newly created database
            var sharedUpn = appConfig.SharedRoomMailboxUpn;
            if (!string.IsNullOrWhiteSpace(sharedUpn))
            {
                sharedUpn = sharedUpn.Trim();
                bool changed = false;
                var rooms = context.Rooms.ToList();
                foreach (var room in rooms)
                {
                    if (!string.Equals(room.MailboxUpn, sharedUpn, StringComparison.OrdinalIgnoreCase))
                    {
                        room.MailboxUpn = sharedUpn;
                        changed = true;
                    }
                }
                if (changed)
                {
                    context.SaveChanges();
                    Console.WriteLine("All room mailbox UPNs set to configured value '{0}' (initial creation).", sharedUpn);
                }
            }
            else
            {
                Console.WriteLine("SharedRoomMailboxUpn configuration value not found; skipping mailbox UPN assignment on new database.");
            }
        }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while ensuring the database was created.");
        throw;
    }
}

app.UseDefaultFiles();
app.UseStaticFiles();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseCors("AllowAll");
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.MapFallbackToFile("/index.html");

app.Run();
