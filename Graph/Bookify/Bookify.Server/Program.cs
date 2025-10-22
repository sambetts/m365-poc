using Azure.Identity;
using Bookify.Server; // For AppConfig
using Bookify.Server.Data;
using Bookify.Server.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Graph;
using FluentValidation;
using FluentValidation.AspNetCore;
using Bookify.Server.Application.Bookings.Validation;
using Bookify.Server.Application.Rooms.Validation;

var builder = WebApplication.CreateBuilder(args);
var appConfig = new AppConfig(builder.Configuration);
if (string.IsNullOrWhiteSpace(appConfig.ConnectionStrings?.DefaultConnection)) throw new InvalidOperationException("ConnectionStrings:DefaultConnection is not configured");
if (string.IsNullOrWhiteSpace(appConfig.AzureAd?.TenantId) || string.IsNullOrWhiteSpace(appConfig.AzureAd.ClientId) || string.IsNullOrWhiteSpace(appConfig.AzureAd.ClientSecret)) throw new InvalidOperationException("AzureAd configuration (TenantId / ClientId / ClientSecret) is incomplete");

builder.Services.AddSingleton(appConfig);
// FluentValidation registration (service collection level)
builder.Services.AddFluentValidationAutoValidation().AddFluentValidationClientsideAdapters();
// MVC Controllers
builder.Services.AddControllers();
// Validators discovery
builder.Services.AddValidatorsFromAssemblyContaining<CreateBookingRequestValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<RoomAvailabilityRequestValidator>();

builder.Services.AddDbContext<BookifyDbContext>(options =>
 options.UseSqlServer(appConfig.ConnectionStrings.DefaultConnection, sqlOptions => sqlOptions.EnableRetryOnFailure()));

builder.Services.AddSingleton<GraphServiceClient>(sp =>
{
 var cfg = sp.GetRequiredService<AppConfig>();
 var credential = new ClientSecretCredential(cfg.AzureAd.TenantId, cfg.AzureAd.ClientId, cfg.AzureAd.ClientSecret);
 return new GraphServiceClient(credential, ["https://graph.microsoft.com/.default"]);
});

builder.Services.AddScoped<IBookingCalendarSyncService, BookingCalendarSyncService>();
builder.Services.AddScoped<IBookingService, BookingService>();
builder.Services.AddScoped<IRoomService, RoomService>();
builder.Services.AddSingleton<IExternalCalendarService, GraphCalendarService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
 options.AddPolicy("AllowAll", corsBuilder => corsBuilder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

var app = builder.Build();

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
 var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");
 logger.LogError(ex, "An error occurred while ensuring the database was created.");
 throw;
 }
}

app.UseDefaultFiles();
app.UseStaticFiles();

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
