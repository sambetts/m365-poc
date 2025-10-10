using Microsoft.EntityFrameworkCore;
using Bookify.Server.Data;
using Bookify.Server.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Configure Entity Framework Core with SQL Server
builder.Services.AddDbContext<BookifyDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions => sqlOptions.EnableRetryOnFailure()
    )
);

// Register application services
builder.Services.AddScoped<IBookingService, BookingService>();
builder.Services.AddScoped<IRoomService, RoomService>();

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

// Automatically apply migrations and create database on startup
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<BookifyDbContext>();

        // Determine if database is new (no applied migrations yet)
        bool isNewDatabase = !context.Database.GetAppliedMigrations().Any();

        context.Database.Migrate();
        Console.WriteLine("Database migration completed successfully.");

        if (isNewDatabase)
        {
            // Assign single configured mailbox UPN to all rooms only for a newly created database
            var sharedUpn = builder.Configuration["SharedRoomMailboxUpn"];
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
        else
        {
            Console.WriteLine("Existing database detected; skipping shared mailbox UPN reassignment.");
        }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating the database.");
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
