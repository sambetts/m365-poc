using Entities.Configuration;
using Microsoft.EntityFrameworkCore;

namespace Entities;

/// <summary>
/// SQL model.
/// </summary>
public class AppDbContext : DbContext
{
    private readonly AppConfig? _config;

    public AppDbContext(AppConfig config)
    {
        this._config = config;
        SetCommandTimeout();
    }
    public AppDbContext(DbContextOptions<AppDbContext> options, AppConfig? config) : base(options)
    {
        this._config = config;
        SetCommandTimeout();
    }


    void SetCommandTimeout()
    {
        const int ONE_HOUR = 3600;
        Database.SetCommandTimeout(ONE_HOUR * 12);
    }

    public DbSet<PlayListItem> PlayList { get; set; } = null!;
    public DbSet<LocationIpRule> LocationIpRules { get; set; } = null!;

    protected override void OnConfiguring(DbContextOptionsBuilder options)
        => options.UseSqlServer(_config!.ConnectionStrings.SQLConnectionString, op => op.EnableRetryOnFailure());
}
