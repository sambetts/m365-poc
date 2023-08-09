using Engine;
using Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Engine;

public interface ILocationIpRuleLoader
{
    Task<IEnumerable<LocationIpRule>> LoadRules();
    string? GetIpAddress();
}
public class SqlAndHttpLocationIpRuleLoader : ILocationIpRuleLoader
{
    private readonly AppDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public SqlAndHttpLocationIpRuleLoader(AppDbContext context, IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<IEnumerable<LocationIpRule>> LoadRules()
    {
        var rules = await _context.LocationIpRules.ToListAsync();
        return rules;
    }


    public string? GetIpAddress()
    {
        string ipAddress = _httpContextAccessor.HttpContext.Request.Headers["HTTP_X_FORWARDED_FOR"];

        if (!string.IsNullOrEmpty(ipAddress))
        {
            string[] addresses = ipAddress.Split(',');
            if (addresses.Length != 0)
            {
                return addresses[0];
            }
        }

        return _httpContextAccessor.HttpContext.Connection?.RemoteIpAddress?.ToString();
    }
}
